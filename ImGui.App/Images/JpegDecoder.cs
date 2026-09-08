// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

/// <summary>
/// Decodes JFIF/EXIF JPEG images to RGBA8.
/// </summary>
/// <remarks>
/// <para>
/// Covers baseline and extended sequential Huffman JPEGs (SOF0/SOF1) and progressive ones (SOF2),
/// with any sampling factors, restart intervals, and one or three components. Arithmetic coding,
/// lossless and hierarchical modes, and four-component CMYK/YCCK are rejected with a message naming
/// what was found; none of them are produced by ordinary image tooling.
/// </para>
/// <para>
/// Coefficients for the whole image are held until every scan has been read — progressive images
/// require it, and sharing the path with baseline keeps one block decoder rather than two. Chroma is
/// upsampled by replication and the inverse DCT is computed in floating point, so results match a
/// reference decoder to within the rounding tolerance such decoders differ by anyway.
/// </para>
/// </remarks>
internal static class JpegDecoder
{
	/// <summary>Reports whether a buffer starts with a JPEG start-of-image marker.</summary>
	/// <param name="data">The candidate bytes.</param>
	/// <returns><see langword="true"/> when the buffer is a JPEG.</returns>
	public static bool IsJpeg(ReadOnlySpan<byte> data) => data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;

	/// <summary>Decodes a JPEG to RGBA8.</summary>
	/// <param name="data">The complete file contents.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The data is not a well formed JPEG this decoder supports.</exception>
	public static ImagePixels Decode(ReadOnlySpan<byte> data)
	{
		Reader reader = new(data);
		return reader.Decode();
	}

	/// <summary>The natural-order index of each zig-zag ordered coefficient.</summary>
	private static ReadOnlySpan<byte> ZigZag =>
	[
		0, 1, 8, 16, 9, 2, 3, 10,
		17, 24, 32, 25, 18, 11, 4, 5,
		12, 19, 26, 33, 40, 48, 41, 34,
		27, 20, 13, 6, 7, 14, 21, 28,
		35, 42, 49, 56, 57, 50, 43, 36,
		29, 22, 15, 23, 30, 37, 44, 51,
		58, 59, 52, 45, 38, 31, 39, 46,
		53, 60, 61, 54, 47, 55, 62, 63,
	];

	private sealed class Component
	{
		public required int Id { get; init; }
		public required int HorizontalSampling { get; init; }
		public required int VerticalSampling { get; init; }
		public required int QuantizationTable { get; init; }

		public int BlocksPerLine { get; set; }
		public int BlocksPerColumn { get; set; }

		/// <summary>The component's real extent in samples, before padding out to whole blocks.</summary>
		public int SampleWidth { get; set; }

		/// <summary>The component's real extent in samples, before padding out to whole blocks.</summary>
		public int SampleHeight { get; set; }
		public short[] Coefficients { get; set; } = [];
		public byte[] Plane { get; set; } = [];
		public int DcTable { get; set; }
		public int AcTable { get; set; }
		public int DcPredictor { get; set; }
	}

	/// <summary>
	/// A canonical Huffman table in the form the spec's DECODE procedure wants: for each code length,
	/// the smallest and largest code of that length and where its symbols start.
	/// </summary>
	private sealed class HuffmanTable
	{
		private readonly int[] minCode = new int[17];
		private readonly int[] maxCode = new int[17];
		private readonly int[] valuePointer = new int[17];
		private readonly byte[] values;

		public HuffmanTable(ReadOnlySpan<byte> counts, byte[] values)
		{
			this.values = values;

			int code = 0;
			int k = 0;
			for (int length = 1; length <= 16; length++)
			{
				valuePointer[length] = k;
				minCode[length] = code;
				code += counts[length - 1];
				k += counts[length - 1];
				maxCode[length] = counts[length - 1] == 0 ? -1 : code - 1;
				code <<= 1;
			}
		}

		public int Decode(ref BitReader bits)
		{
			int code = 0;
			for (int length = 1; length <= 16; length++)
			{
				code = (code << 1) | bits.ReadBit();
				if (maxCode[length] >= 0 && code <= maxCode[length])
				{
					int index = valuePointer[length] + code - minCode[length];
					return index < values.Length
						? values[index]
						: throw new InvalidImageDataException("JPEG Huffman code resolves past the end of its symbol table.");
				}
			}

			throw new InvalidImageDataException("JPEG entropy data contains a Huffman code longer than 16 bits.");
		}
	}

	/// <summary>
	/// Feeds entropy-coded bits, unstuffing the zero byte the format inserts after each 0xFF and
	/// stopping cleanly at a marker rather than reading into it.
	/// </summary>
	private ref struct BitReader
	{
		private readonly ReadOnlySpan<byte> data;
		private int position;
		private int buffer;
		private int count;

		public BitReader(ReadOnlySpan<byte> data, int position)
		{
			this.data = data;
			this.position = position;
		}

		/// <summary>Gets the offset of the next unread byte, once the reader has stopped at a marker.</summary>
		public readonly int Position => position;

		public int ReadBit()
		{
			if (count == 0)
			{
				FillByte();
			}

			count--;
			return (buffer >> count) & 1;
		}

		public int ReadBits(int length)
		{
			int value = 0;
			for (int i = 0; i < length; i++)
			{
				value = (value << 1) | ReadBit();
			}

			return value;
		}

		/// <summary>Drops any partial byte and steps over the restart marker that follows it.</summary>
		public void Restart()
		{
			count = 0;

			while (position + 1 < data.Length)
			{
				if (data[position] == 0xFF && data[position + 1] is >= 0xD0 and <= 0xD7)
				{
					position += 2;
					return;
				}

				position++;
			}
		}

		private void FillByte()
		{
			if (position >= data.Length)
			{
				// Past the end, feed zero bits: a truncated final block decodes to something rather
				// than throwing away the whole image.
				buffer = 0;
				count = 8;
				return;
			}

			byte value = data[position];
			if (value == 0xFF)
			{
				byte next = position + 1 < data.Length ? data[position + 1] : (byte)0xD9;
				if (next == 0x00)
				{
					position += 2;
				}
				else
				{
					// A real marker starts here, so the entropy data is over. Leave the position on it
					// for the segment reader and pad the rest of this block with zero bits.
					buffer = 0;
					count = 8;
					return;
				}
			}
			else
			{
				position++;
			}

			buffer = value;
			count = 8;
		}
	}

	private ref struct Reader
	{
		private readonly ReadOnlySpan<byte> data;
		private int position;

		private readonly ushort[]?[] quantizationTables = new ushort[4][];
		private readonly HuffmanTable?[] dcTables = new HuffmanTable?[4];
		private readonly HuffmanTable?[] acTables = new HuffmanTable?[4];
		private readonly List<Component> components = [];

		private int width;
		private int height;
		private int maxHorizontalSampling = 1;
		private int maxVerticalSampling = 1;
		private int mcusPerLine;
		private int mcusPerColumn;
		private int restartInterval;
		private bool progressive;
		private bool frameStarted;
		private int adobeTransform = -1;
		private int eobRun;

		/// <summary>Sink for blocks that fall outside a component's real extent but still consume entropy data.</summary>
		private readonly short[] paddingBlock = new short[64];

		public Reader(ReadOnlySpan<byte> data)
		{
			this.data = data;
			position = 2;
		}

		public ImagePixels Decode()
		{
			if (!IsJpeg(data))
			{
				throw new InvalidImageDataException("Not a JPEG: the file does not start with a start-of-image marker.");
			}

			while (position + 1 < data.Length)
			{
				if (data[position] != 0xFF)
				{
					position++;
					continue;
				}

				byte marker = data[position + 1];
				position += 2;

				switch (marker)
				{
					case 0xFF:
						// Fill byte before the real marker; back up so the next iteration sees it.
						position--;
						continue;

					case 0x01:
					case >= 0xD0 and <= 0xD7:
						continue;

					case 0xD9:
						return Finish();

					case 0xC4:
						ReadHuffmanTables(ReadSegment());
						continue;

					case 0xDB:
						ReadQuantizationTables(ReadSegment());
						continue;

					case 0xDD:
					{
						ReadOnlySpan<byte> segment = ReadSegment();
						restartInterval = segment.Length >= 2 ? BinaryPrimitives.ReadUInt16BigEndian(segment) : 0;
						continue;
					}

					case 0xEE:
						ReadAdobeMarker(ReadSegment());
						continue;

					case 0xC0:
					case 0xC1:
					case 0xC2:
						ReadFrame(ReadSegment(), progressiveFrame: marker == 0xC2);
						continue;

					case 0xC3:
					case >= 0xC5 and <= 0xC7:
					case >= 0xC9 and <= 0xCB:
					case >= 0xCD and <= 0xCF:
						throw new InvalidImageDataException($"JPEG uses unsupported coding process (SOF marker 0x{marker:X2}); only baseline, extended sequential and progressive Huffman are supported.");

					case 0xDA:
						ReadScan(ReadSegment());
						continue;

					default:
						ReadSegment();
						continue;
				}
			}

			return Finish();
		}

		private ReadOnlySpan<byte> ReadSegment()
		{
			if (position + 2 > data.Length)
			{
				throw new InvalidImageDataException("JPEG segment header runs past the end of the file.");
			}

			int length = BinaryPrimitives.ReadUInt16BigEndian(data[position..]);
			if (length < 2 || position + length > data.Length)
			{
				throw new InvalidImageDataException($"JPEG segment declares a length of {length} bytes, which does not fit the file.");
			}

			ReadOnlySpan<byte> segment = data.Slice(position + 2, length - 2);
			position += length;
			return segment;
		}

		private void ReadAdobeMarker(ReadOnlySpan<byte> segment)
		{
			if (segment.Length >= 12 && segment[..5].SequenceEqual("Adobe"u8))
			{
				adobeTransform = segment[11];
			}
		}

		private readonly void ReadQuantizationTables(ReadOnlySpan<byte> segment)
		{
			int offset = 0;
			while (offset < segment.Length)
			{
				int precision = segment[offset] >> 4;
				int id = segment[offset] & 0x0F;
				offset++;

				if (id >= 4)
				{
					throw new InvalidImageDataException($"JPEG quantization table id {id} is out of range.");
				}

				int entryBytes = precision == 0 ? 1 : 2;
				if (offset + (64 * entryBytes) > segment.Length)
				{
					throw new InvalidImageDataException("JPEG quantization table runs past the end of its segment.");
				}

				ushort[] table = new ushort[64];
				for (int i = 0; i < 64; i++)
				{
					table[i] = entryBytes == 1
						? segment[offset + i]
						: BinaryPrimitives.ReadUInt16BigEndian(segment[(offset + (i * 2))..]);
				}

				quantizationTables[id] = table;
				offset += 64 * entryBytes;
			}
		}

		private readonly void ReadHuffmanTables(ReadOnlySpan<byte> segment)
		{
			int offset = 0;
			while (offset < segment.Length)
			{
				int tableClass = segment[offset] >> 4;
				int id = segment[offset] & 0x0F;
				offset++;

				if (id >= 4 || tableClass > 1)
				{
					throw new InvalidImageDataException($"JPEG Huffman table class {tableClass} id {id} is out of range.");
				}

				if (offset + 16 > segment.Length)
				{
					throw new InvalidImageDataException("JPEG Huffman table runs past the end of its segment.");
				}

				ReadOnlySpan<byte> counts = segment.Slice(offset, 16);
				offset += 16;

				int total = 0;
				foreach (byte count in counts)
				{
					total += count;
				}

				if (offset + total > segment.Length)
				{
					throw new InvalidImageDataException("JPEG Huffman symbol table runs past the end of its segment.");
				}

				byte[] values = segment.Slice(offset, total).ToArray();
				offset += total;

				HuffmanTable table = new(counts, values);
				if (tableClass == 0)
				{
					dcTables[id] = table;
				}
				else
				{
					acTables[id] = table;
				}
			}
		}

		private void ReadFrame(ReadOnlySpan<byte> segment, bool progressiveFrame)
		{
			if (frameStarted)
			{
				throw new InvalidImageDataException("JPEG contains more than one frame; hierarchical mode is not supported.");
			}

			if (segment.Length < 6)
			{
				throw new InvalidImageDataException("JPEG frame header is too short.");
			}

			int precision = segment[0];
			if (precision != 8)
			{
				throw new InvalidImageDataException($"JPEG uses {precision}-bit samples; only 8-bit is supported.");
			}

			height = BinaryPrimitives.ReadUInt16BigEndian(segment[1..]);
			width = BinaryPrimitives.ReadUInt16BigEndian(segment[3..]);
			int componentCount = segment[5];

			if (width == 0 || height == 0)
			{
				throw new InvalidImageDataException($"JPEG has unusable dimensions {width}x{height}.");
			}

			if (componentCount is not (1 or 3))
			{
				throw new InvalidImageDataException($"JPEG has {componentCount} components; only greyscale and three-component colour are supported.");
			}

			if (segment.Length < 6 + (componentCount * 3))
			{
				throw new InvalidImageDataException("JPEG frame header is missing component descriptions.");
			}

			for (int i = 0; i < componentCount; i++)
			{
				int offset = 6 + (i * 3);
				int horizontal = segment[offset + 1] >> 4;
				int vertical = segment[offset + 1] & 0x0F;
				if (horizontal is < 1 or > 4 || vertical is < 1 or > 4)
				{
					throw new InvalidImageDataException($"JPEG component {i} declares invalid sampling factors {horizontal}x{vertical}.");
				}

				components.Add(new Component
				{
					Id = segment[offset],
					HorizontalSampling = horizontal,
					VerticalSampling = vertical,
					QuantizationTable = segment[offset + 2],
				});

				maxHorizontalSampling = Math.Max(maxHorizontalSampling, horizontal);
				maxVerticalSampling = Math.Max(maxVerticalSampling, vertical);
			}

			mcusPerLine = CeilDiv(width, 8 * maxHorizontalSampling);
			mcusPerColumn = CeilDiv(height, 8 * maxVerticalSampling);

			foreach (Component component in components)
			{
				component.BlocksPerLine = mcusPerLine * component.HorizontalSampling;
				component.BlocksPerColumn = mcusPerColumn * component.VerticalSampling;
				component.SampleWidth = CeilDiv(width * component.HorizontalSampling, maxHorizontalSampling);
				component.SampleHeight = CeilDiv(height * component.VerticalSampling, maxVerticalSampling);
				component.Coefficients = new short[component.BlocksPerLine * component.BlocksPerColumn * 64];
			}

			progressive = progressiveFrame;
			frameStarted = true;
		}

		private void ReadScan(ReadOnlySpan<byte> header)
		{
			if (!frameStarted)
			{
				throw new InvalidImageDataException("JPEG scan appears before its frame header.");
			}

			if (header.Length < 1)
			{
				throw new InvalidImageDataException("JPEG scan header is too short.");
			}

			int scanComponentCount = header[0];
			if (scanComponentCount < 1 || header.Length < 1 + (scanComponentCount * 2) + 3)
			{
				throw new InvalidImageDataException("JPEG scan header is malformed.");
			}

			List<Component> scanComponents = [];
			for (int i = 0; i < scanComponentCount; i++)
			{
				int id = header[1 + (i * 2)];
				int tables = header[2 + (i * 2)];
				Component component = components.Find(c => c.Id == id)
					?? throw new InvalidImageDataException($"JPEG scan references component {id}, which the frame does not declare.");

				component.DcTable = tables >> 4;
				component.AcTable = tables & 0x0F;
				scanComponents.Add(component);
			}

			int spectralStart = header[1 + (scanComponentCount * 2)];
			int spectralEnd = header[2 + (scanComponentCount * 2)];
			int approximation = header[3 + (scanComponentCount * 2)];
			int successiveHigh = approximation >> 4;
			int successiveLow = approximation & 0x0F;

			if (!progressive)
			{
				spectralStart = 0;
				spectralEnd = 63;
				successiveHigh = 0;
				successiveLow = 0;
			}

			eobRun = 0;
			foreach (Component component in scanComponents)
			{
				component.DcPredictor = 0;
			}

			BitReader bits = new(data, position);
			DecodeScan(ref bits, scanComponents, spectralStart, spectralEnd, successiveHigh, successiveLow);
			position = Math.Max(position, bits.Position);
		}

		private void DecodeScan(ref BitReader bits, List<Component> scanComponents, int spectralStart, int spectralEnd, int successiveHigh, int successiveLow)
		{
			bool interleaved = scanComponents.Count > 1;
			int unitsPerLine;
			int unitsPerColumn;

			if (interleaved)
			{
				unitsPerLine = mcusPerLine;
				unitsPerColumn = mcusPerColumn;
			}
			else
			{
				// A single-component scan walks that component's own blocks, which cover only the
				// component's real extent rather than the padded MCU grid.
				Component only = scanComponents[0];
				unitsPerLine = CeilDiv(CeilDiv(width * only.HorizontalSampling, maxHorizontalSampling), 8);
				unitsPerColumn = CeilDiv(CeilDiv(height * only.VerticalSampling, maxVerticalSampling), 8);
			}

			int sinceRestart = 0;
			for (int row = 0; row < unitsPerColumn; row++)
			{
				for (int column = 0; column < unitsPerLine; column++)
				{
					if (restartInterval > 0 && sinceRestart == restartInterval)
					{
						bits.Restart();
						sinceRestart = 0;
						eobRun = 0;
						foreach (Component component in scanComponents)
						{
							component.DcPredictor = 0;
						}
					}

					if (interleaved)
					{
						foreach (Component component in scanComponents)
						{
							for (int v = 0; v < component.VerticalSampling; v++)
							{
								for (int h = 0; h < component.HorizontalSampling; h++)
								{
									int blockRow = (row * component.VerticalSampling) + v;
									int blockColumn = (column * component.HorizontalSampling) + h;
									DecodeBlock(ref bits, component, blockRow, blockColumn, spectralStart, spectralEnd, successiveHigh, successiveLow);
								}
							}
						}
					}
					else
					{
						DecodeBlock(ref bits, scanComponents[0], row, column, spectralStart, spectralEnd, successiveHigh, successiveLow);
					}

					sinceRestart++;
				}
			}
		}

		private void DecodeBlock(ref BitReader bits, Component component, int blockRow, int blockColumn, int spectralStart, int spectralEnd, int successiveHigh, int successiveLow)
		{
			if (blockRow >= component.BlocksPerColumn || blockColumn >= component.BlocksPerLine)
			{
				// Padding blocks past the component's real extent still consume entropy data.
				DecodeInto(ref bits, component, paddingBlock, spectralStart, spectralEnd, successiveHigh, successiveLow);
				return;
			}

			int offset = ((blockRow * component.BlocksPerLine) + blockColumn) * 64;
			DecodeInto(ref bits, component, component.Coefficients.AsSpan(offset, 64), spectralStart, spectralEnd, successiveHigh, successiveLow);
		}

		private void DecodeInto(ref BitReader bits, Component component, Span<short> block, int spectralStart, int spectralEnd, int successiveHigh, int successiveLow)
		{
			HuffmanTable? dc = dcTables[component.DcTable];
			HuffmanTable? ac = acTables[component.AcTable];

			if (!progressive)
			{
				DecodeBaselineBlock(ref bits, component, block, dc, ac);
				return;
			}

			if (spectralStart == 0)
			{
				DecodeProgressiveDc(ref bits, component, block, dc, successiveHigh, successiveLow);
			}
			else
			{
				DecodeProgressiveAc(ref bits, block, ac, spectralStart, spectralEnd, successiveHigh, successiveLow);
			}
		}

		private static void DecodeBaselineBlock(ref BitReader bits, Component component, Span<short> block, HuffmanTable? dc, HuffmanTable? ac)
		{
			if (dc is null || ac is null)
			{
				throw new InvalidImageDataException("JPEG scan references a Huffman table the file never defined.");
			}

			block.Clear();

			int magnitude = dc.Decode(ref bits);
			int diff = magnitude == 0 ? 0 : Extend(bits.ReadBits(magnitude), magnitude);
			component.DcPredictor += diff;
			block[0] = (short)component.DcPredictor;

			int k = 1;
			while (k < 64)
			{
				int rs = ac.Decode(ref bits);
				int size = rs & 15;
				int run = rs >> 4;

				if (size == 0)
				{
					if (run != 15)
					{
						break;
					}

					k += 16;
					continue;
				}

				k += run;
				if (k > 63)
				{
					break;
				}

				block[ZigZag[k]] = (short)Extend(bits.ReadBits(size), size);
				k++;
			}
		}

		private static void DecodeProgressiveDc(ref BitReader bits, Component component, Span<short> block, HuffmanTable? dc, int successiveHigh, int successiveLow)
		{
			if (successiveHigh == 0)
			{
				if (dc is null)
				{
					throw new InvalidImageDataException("JPEG scan references a DC Huffman table the file never defined.");
				}

				block.Clear();
				int magnitude = dc.Decode(ref bits);
				int diff = magnitude == 0 ? 0 : Extend(bits.ReadBits(magnitude), magnitude);
				component.DcPredictor += diff;
				block[0] = (short)(component.DcPredictor << successiveLow);
			}
			else if (bits.ReadBit() != 0)
			{
				block[0] |= (short)(1 << successiveLow);
			}
		}

		private void DecodeProgressiveAc(ref BitReader bits, Span<short> block, HuffmanTable? ac, int spectralStart, int spectralEnd, int successiveHigh, int successiveLow)
		{
			if (ac is null)
			{
				throw new InvalidImageDataException("JPEG scan references an AC Huffman table the file never defined.");
			}

			if (successiveHigh == 0)
			{
				DecodeProgressiveAcFirst(ref bits, block, ac, spectralStart, spectralEnd, successiveLow);
			}
			else
			{
				DecodeProgressiveAcRefine(ref bits, block, ac, spectralStart, spectralEnd, successiveLow);
			}
		}

		private void DecodeProgressiveAcFirst(ref BitReader bits, Span<short> block, HuffmanTable ac, int spectralStart, int spectralEnd, int successiveLow)
		{
			if (eobRun > 0)
			{
				eobRun--;
				return;
			}

			int k = spectralStart;
			while (k <= spectralEnd)
			{
				int rs = ac.Decode(ref bits);
				int size = rs & 15;
				int run = rs >> 4;

				if (size == 0)
				{
					if (run < 15)
					{
						eobRun = (1 << run) - 1;
						if (run > 0)
						{
							eobRun += bits.ReadBits(run);
						}

						break;
					}

					k += 16;
					continue;
				}

				k += run;
				if (k > spectralEnd)
				{
					break;
				}

				block[ZigZag[k]] = (short)(Extend(bits.ReadBits(size), size) * (1 << successiveLow));
				k++;
			}
		}

		private void DecodeProgressiveAcRefine(ref BitReader bits, Span<short> block, HuffmanTable ac, int spectralStart, int spectralEnd, int successiveLow)
		{
			short bit = (short)(1 << successiveLow);

			if (eobRun > 0)
			{
				eobRun--;
				for (int band = spectralStart; band <= spectralEnd; band++)
				{
					RefineCoefficient(ref bits, block, ZigZag[band], bit);
				}

				return;
			}

			int k = spectralStart;
			while (k <= spectralEnd)
			{
				int rs = ac.Decode(ref bits);
				int size = rs & 15;
				int run = rs >> 4;
				int newValue = 0;

				if (size == 0)
				{
					if (run < 15)
					{
						eobRun = (1 << run) - 1;
						if (run > 0)
						{
							eobRun += bits.ReadBits(run);
						}

						// Force the correction loop below to run to the end of the band.
						run = 64;
					}
				}
				else
				{
					if (size != 1)
					{
						throw new InvalidImageDataException("JPEG progressive refinement scan contains a coefficient magnitude other than one.");
					}

					newValue = bits.ReadBit() != 0 ? bit : -bit;
				}

				while (k <= spectralEnd)
				{
					int index = ZigZag[k];
					k++;

					if (block[index] != 0)
					{
						RefineCoefficient(ref bits, block, index, bit);
					}
					else
					{
						if (run == 0)
						{
							if (newValue != 0)
							{
								block[index] = (short)newValue;
							}

							break;
						}

						run--;
					}
				}
			}
		}

		private static void RefineCoefficient(ref BitReader bits, Span<short> block, int index, short bit)
		{
			if (block[index] == 0 || bits.ReadBit() == 0 || (block[index] & bit) != 0)
			{
				return;
			}

			block[index] += block[index] > 0 ? bit : (short)-bit;
		}

		private static int Extend(int value, int magnitude) =>
			value < (1 << (magnitude - 1)) ? value - (1 << magnitude) + 1 : value;

		private readonly ImagePixels Finish()
		{
			if (!frameStarted)
			{
				throw new InvalidImageDataException("JPEG contains no frame header.");
			}

			foreach (Component component in components)
			{
				ushort[]? quantization = component.QuantizationTable < 4 ? quantizationTables[component.QuantizationTable] : null;
				BuildPlane(component, quantization ?? throw new InvalidImageDataException(
					$"JPEG component {component.Id} references quantization table {component.QuantizationTable}, which the file never defined."));
			}

			return Compose();
		}

		private static void BuildPlane(Component component, ushort[] quantization)
		{
			int planeWidth = component.BlocksPerLine * 8;
			int planeHeight = component.BlocksPerColumn * 8;
			byte[] plane = new byte[planeWidth * planeHeight];

			Span<float> dequantized = stackalloc float[64];
			Span<byte> samples = stackalloc byte[64];

			for (int blockRow = 0; blockRow < component.BlocksPerColumn; blockRow++)
			{
				for (int blockColumn = 0; blockColumn < component.BlocksPerLine; blockColumn++)
				{
					int offset = ((blockRow * component.BlocksPerLine) + blockColumn) * 64;
					ReadOnlySpan<short> block = component.Coefficients.AsSpan(offset, 64);

					// Flat blocks are the common case in real photographs, and their transform is a
					// constant, so skip the whole 8x8 pass for them.
					if (IsDcOnly(block))
					{
						samples.Fill(DcOnlySample(block[0] * (float)quantization[0]));
					}
					else
					{
						for (int k = 0; k < 64; k++)
						{
							dequantized[ZigZag[k]] = block[ZigZag[k]] * (float)quantization[k];
						}

						InverseDct.Transform(dequantized, samples);
					}

					for (int y = 0; y < 8; y++)
					{
						int destination = (((blockRow * 8) + y) * planeWidth) + (blockColumn * 8);
						samples.Slice(y * 8, 8).CopyTo(plane.AsSpan(destination, 8));
					}
				}
			}

			component.Plane = plane;
		}

		private static bool IsDcOnly(ReadOnlySpan<short> block)
		{
			for (int i = 1; i < 64; i++)
			{
				if (block[i] != 0)
				{
					return false;
				}
			}

			return true;
		}

		private static byte DcOnlySample(float dc)
		{
			// The transform of a DC-only block is that coefficient spread evenly, which the basis
			// normalization reduces to a division by eight.
			int value = (int)MathF.Round((dc / 8f) + 128f);
			return value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)value;
		}

		private readonly ImagePixels Compose()
		{
			ImagePixels image = new(width, height);
			Span<byte> pixels = image.Pixels;

			if (components.Count == 1)
			{
				Component only = components[0];
				int planeWidth = only.BlocksPerLine * 8;
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						byte grey = only.Plane[(y * planeWidth) + x];
						int i = ((y * width) + x) * 4;
						pixels[i] = grey;
						pixels[i + 1] = grey;
						pixels[i + 2] = grey;
						pixels[i + 3] = 255;
					}
				}

				return image;
			}

			// An Adobe marker with transform 0, or components literally labelled R, G and B, means the
			// samples are already RGB and must not go through the YCbCr matrix.
			bool rgbDirect = adobeTransform == 0
				|| (components[0].Id == 'R' && components[1].Id == 'G' && components[2].Id == 'B');

			Upsampler[] upsamplers =
			[
				new(components[0], width, height, maxHorizontalSampling, maxVerticalSampling),
				new(components[1], width, height, maxHorizontalSampling, maxVerticalSampling),
				new(components[2], width, height, maxHorizontalSampling, maxVerticalSampling),
			];

			for (int y = 0; y < height; y++)
			{
				foreach (Upsampler upsampler in upsamplers)
				{
					upsampler.BeginRow(y);
				}

				for (int x = 0; x < width; x++)
				{
					float s0 = upsamplers[0].Sample(x);
					float s1 = upsamplers[1].Sample(x);
					float s2 = upsamplers[2].Sample(x);

					int i = ((y * width) + x) * 4;
					if (rgbDirect)
					{
						pixels[i] = Clamp(s0);
						pixels[i + 1] = Clamp(s1);
						pixels[i + 2] = Clamp(s2);
					}
					else
					{
						float cb = s1 - 128f;
						float cr = s2 - 128f;
						pixels[i] = Clamp(s0 + (1.402f * cr));
						pixels[i + 1] = Clamp(s0 - (0.344136f * cb) - (0.714136f * cr));
						pixels[i + 2] = Clamp(s0 + (1.772f * cb));
					}

					pixels[i + 3] = 255;
				}
			}

			return image;
		}

		private static byte Clamp(float value)
		{
			int rounded = (int)MathF.Round(value);
			return rounded <= 0 ? (byte)0 : rounded >= 255 ? (byte)255 : (byte)rounded;
		}

		private static int CeilDiv(int value, int divisor) => (value + divisor - 1) / divisor;
	}

	/// <summary>
	/// Reads a component's plane at full image resolution, interpolating between samples when the
	/// component is subsampled.
	/// </summary>
	/// <remarks>
	/// Sample centres are offset by half a sample, so a chroma sample sits in the middle of the block
	/// of luma pixels it covers. Bilinear interpolation on that grid is the same triangle filter a
	/// reference decoder applies for the usual 2x factors, and it degrades sensibly for the unusual
	/// ones rather than needing a special case per sampling factor.
	/// </remarks>
	private sealed class Upsampler
	{
		private readonly byte[] plane;
		private readonly int planeWidth;
		private readonly int sampleHeight;
		private readonly int verticalSampling;
		private readonly int maxVerticalSampling;
		private readonly int[] leftColumn;
		private readonly int[] rightColumn;
		private readonly float[] columnWeight;
		private readonly bool subsampled;

		private int topOffset;
		private int bottomOffset;
		private float rowWeight;

		public Upsampler(Component component, int width, int height, int maxHorizontalSampling, int maxVerticalSampling)
		{
			plane = component.Plane;
			planeWidth = component.BlocksPerLine * 8;
			sampleHeight = Math.Min(component.SampleHeight, component.BlocksPerColumn * 8);
			verticalSampling = component.VerticalSampling;
			this.maxVerticalSampling = maxVerticalSampling;

			subsampled = component.HorizontalSampling != maxHorizontalSampling
				|| component.VerticalSampling != maxVerticalSampling;

			int sampleWidth = Math.Min(component.SampleWidth, planeWidth);
			leftColumn = new int[width];
			rightColumn = new int[width];
			columnWeight = new float[width];

			for (int x = 0; x < width; x++)
			{
				double centre = ((x + 0.5) * component.HorizontalSampling / maxHorizontalSampling) - 0.5;
				int left = (int)Math.Floor(centre);
				columnWeight[x] = (float)(centre - left);
				leftColumn[x] = Math.Clamp(left, 0, sampleWidth - 1);
				rightColumn[x] = Math.Clamp(left + 1, 0, sampleWidth - 1);
			}

			_ = height;
		}

		/// <summary>Selects the two source rows, and their blend, for one output row.</summary>
		/// <param name="y">The output row.</param>
		public void BeginRow(int y)
		{
			double centre = ((y + 0.5) * verticalSampling / maxVerticalSampling) - 0.5;
			int top = (int)Math.Floor(centre);
			rowWeight = (float)(centre - top);
			topOffset = Math.Clamp(top, 0, sampleHeight - 1) * planeWidth;
			bottomOffset = Math.Clamp(top + 1, 0, sampleHeight - 1) * planeWidth;
		}

		/// <summary>Reads one interpolated sample from the row selected by <see cref="BeginRow"/>.</summary>
		/// <param name="x">The output column.</param>
		/// <returns>The interpolated sample value.</returns>
		public float Sample(int x)
		{
			// A component at full resolution samples one to one, and the interpolation below would
			// only ever pick that same pixel with a weight of zero.
			if (!subsampled)
			{
				return plane[topOffset + x];
			}

			int left = leftColumn[x];
			int right = rightColumn[x];
			float weight = columnWeight[x];

			float top = plane[topOffset + left] + ((plane[topOffset + right] - plane[topOffset + left]) * weight);
			float bottom = plane[bottomOffset + left] + ((plane[bottomOffset + right] - plane[bottomOffset + left]) * weight);
			return top + ((bottom - top) * rowWeight);
		}
	}

	/// <summary>
	/// The 8x8 inverse discrete cosine transform, computed separably in floating point against a
	/// precomputed basis.
	/// </summary>
	private static class InverseDct
	{
		private static readonly float[] Basis = BuildBasis();

		public static void Transform(ReadOnlySpan<float> coefficients, Span<byte> samples)
		{
			Span<float> rows = stackalloc float[64];

			// Pass one: for every coefficient row u, transform along v into spatial x.
			for (int u = 0; u < 8; u++)
			{
				for (int x = 0; x < 8; x++)
				{
					float sum = 0f;
					for (int v = 0; v < 8; v++)
					{
						sum += coefficients[(u * 8) + v] * Basis[(v * 8) + x];
					}

					rows[(u * 8) + x] = sum;
				}
			}

			// Pass two: transform along u into spatial y, then shift out of the signed range.
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					float sum = 0f;
					for (int u = 0; u < 8; u++)
					{
						sum += rows[(u * 8) + x] * Basis[(u * 8) + y];
					}

					int value = (int)MathF.Round(sum + 128f);
					samples[(y * 8) + x] = value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)value;
				}
			}
		}

		private static float[] BuildBasis()
		{
			float[] basis = new float[64];
			for (int u = 0; u < 8; u++)
			{
				double normalization = u == 0 ? Math.Sqrt(0.125) : 0.5;
				for (int x = 0; x < 8; x++)
				{
					basis[(u * 8) + x] = (float)(normalization * Math.Cos(((2 * x) + 1) * u * Math.PI / 16.0));
				}
			}

			return basis;
		}
	}
}
