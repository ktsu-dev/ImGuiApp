namespace ktsu.io
{
	public static class Extensions
	{
		//from https://thomaslevesque.com/2019/11/18/using-foreach-with-index-in-c/
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) => source.Select((item, index) => (item, index));
	}
}
