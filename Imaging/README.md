# ktsu.Imaging

Imaging decodes, edits and writes images with no third-party dependency. It has no UI or graphics API and does not know that Dear ImGui exists. `ktsu.ImGui.App` uses it for textures, window icons and the image clipboard. Command-line tools use it in place of `SixLabors.ImageSharp`.

## Why it exists

`SixLabors.ImageSharp` moved to a split licence at 4.0, and its 3.1.x line now fails restore on NU1902/NU1903 security advisories, which are errors under `TreatWarningsAsErrors`. Its 3.2.0 release wants a licence key in Release builds. This package does only what ktsu projects need, written so a dependency's licence or advisories can never block them.

## Installation

```bash
dotnet add package ktsu.Imaging
```

## Quick Start

```csharp
using ktsu.Imaging;

ImagePixels icon = ImageDecoder.Load("icon.jpg");

ImagePixels square = icon.CropToSquare().Resize(256, 256);
ImagePixels padded = square.Pad(288, 288, 0, 0, 0, 0);

PngEncoder.Save(padded, "icon.png", new PngEncoderOptions { ClearTransparentPixels = true });
```

## The pixel buffer

`ImagePixels` is a tightly packed RGBA8 buffer with straight (not premultiplied) alpha. Rows run top to bottom with no padding, so pixel `(x, y)` starts at byte `(y * Width + x) * 4`.

| Member | What it does |
|---|---|
| `Width`, `Height`, `ByteLength` | Dimensions, and `Width * Height * 4` |
| `Pixels`, `ReadOnlyPixels`, `GetBuffer()` | The whole buffer as a span, or the backing array |
| `GetRow(y)` | One row as a writable span |
| `GetPixel(x, y)`, `SetPixel(x, y, r, g, b, a)` | One pixel |
| `Fill(r, g, b, a)` | Every pixel |
| `Crop(x, y, width, height)` | A new image holding that rectangle |
| `CropToSquare()` | The centred square of the shorter side |
| `Pad(width, height, r, g, b, a)` | A larger canvas in the given colour, with the image centred and unscaled. An odd extra pixel goes to the right and bottom |
| `Resize(width, height)` | Lanczos-3 resampling on premultiplied alpha, which averages rather than point-samples when shrinking |
| `Clone()`, `CopyPixelDataTo(span)` | Copies |

## Decoding

`ImageDecoder.Load(path)`, `Load(stream)` and `Decode(bytes)` return an `ImagePixels`. `ImageDecoder.Identify(bytes)` returns the `ImageFormat` read from the leading bytes. Unrecognised or malformed input raises `InvalidImageDataException`, which names what was found.

| Format | Covered | Not covered |
|---|---|---|
| PNG | All five colour types, bit depths 1/2/4/8/16, palettes, `tRNS`, Adam7, all five filters | Colour management: `gAMA` and `iCCP` are ignored |
| JPEG | Baseline, extended sequential and progressive Huffman, any sampling factors, restart intervals, greyscale and YCbCr, the Adobe transform flag | Arithmetic coding, lossless and hierarchical modes, CMYK/YCCK, 12-bit samples |
| BMP | Core and info headers, 1/4/8/16/24/32 bit, top-down and bottom-up, `BI_RGB` and `BI_BITFIELDS` | `BI_RLE4`/`BI_RLE8`, embedded PNG or JPEG payloads |
| TGA | Colour-mapped, true-colour and greyscale, raw and run-length encoded | Nothing in common use |

Only the first frame of a file is decoded. Sixteen-bit PNG samples keep their high byte. A 32-bit BMP or TGA whose alpha is zero everywhere is treated as opaque.

## Encoding

`PngEncoder.Encode(pixels)` returns the bytes of an eight-bit RGBA PNG. `Encode(pixels, stream)` writes them to a stream and leaves it open, and `Save(pixels, path)` writes them to a file. `PngEncoderOptions` sets:

- `CompressionLevel`: defaults to `Optimal`.
- `ClearTransparentPixels`: zeroes the colour of fully transparent pixels in the file, which is ImageSharp's `TransparentColorMode.Clear`. The source buffer is not changed.

There is no JPEG, BMP or TGA encoder.

## Other pixel layouts

`PixelConverter.ToRgba8(source, width, height, layout, stride, destination)` converts a buffer in any `PixelLayout` to RGBA8: `Rgba8`, `Bgra8`, `Rgb8`, `Bgr8` or `Gray8`. `stride` is the bytes from the start of one row to the start of the next, or `0` for tightly packed rows. It checks the buffer first and throws if the stride is shorter than a row, the source is too short, or `destination` is shorter than `width * height * 4` bytes. `PixelConverter.Validate` runs the same check on its own and returns the effective stride, and `BytesPerPixel(layout)` gives the pixel size.

## Coming from ImageSharp

| ImageSharp | ktsu.Imaging |
|---|---|
| `Image.Load<Rgba32>(path)` | `ImageDecoder.Load(path)` |
| `Image.DetectFormat` | `ImageDecoder.Identify(bytes)` |
| `new Image<Rgba32>(w, h, color)` | `new ImagePixels(w, h)` then `Fill(...)` |
| `image[x, y]` | `GetPixel` / `SetPixel` |
| `ProcessPixelRows` / `GetRowSpan(y)` | `GetRow(y)` or `Pixels` |
| `Mutate(x => x.Crop(rect))` | `Crop(x, y, w, h)` |
| `Mutate(x => x.Pad(w, h, color))` | `Pad(w, h, r, g, b, a)` |
| `Mutate(x => x.Resize(w, h))` | `Resize(w, h)`, Lanczos-3 rather than ImageSharp's default bicubic, so output differs slightly |
| `SaveAsPng(path, new PngEncoder { ... TransparentColorMode.Clear })` | `PngEncoder.Save(pixels, path, new PngEncoderOptions { ClearTransparentPixels = true })` |
| `ImageProcessingException`, `InvalidImageContentException`, `UnknownImageFormatException` | `InvalidImageDataException` |

Filters such as `BlackWhite` are not provided. Each is a few lines over `Pixels`.
