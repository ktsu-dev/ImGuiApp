namespace ktsu.io
{
	public class DividerZone
	{
		public string Id { get; private set; }
		public float Size { get; set; }
		public bool Resizable { get; } = true;
		private Action<float>? TickDelegate { get; }

		public DividerZone(string id, float size)
		{
			Id = id;
			Size = size;
			Resizable = true;
		}

		public DividerZone(string id, float size, Action<float> tickDelegate)
		{
			Id = id;
			Size = size;
			TickDelegate = tickDelegate;
		}

		public DividerZone(string id, float size, bool resizable, Action<float> tickDelegate)
		{
			Id = id;
			Size = size;
			Resizable = resizable;
			TickDelegate = tickDelegate;
		}

		public DividerZone(string id, float size, bool resizable)
		{
			Id = id;
			Size = size;
			Resizable = resizable;
		}

		internal void Tick(float dt) => TickDelegate?.Invoke(dt);
	}
}
