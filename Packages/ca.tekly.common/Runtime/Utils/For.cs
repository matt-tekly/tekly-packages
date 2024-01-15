using System;

namespace Tekly.Common.Utils
{
	public static class For
	{
		public static void Do(int x, Action<int> action)
		{
			for (var i = 0; i < x; i++) {
				action(i);
			}
		}

		public static void Do(int x, int y, Action<int, int> action)
		{
			for (var i = 0; i < x; i++) {
				for (var j = 0; j < y; j++) {
					action(i, j);
				}
			}
		}
		
		public static void Do(this (int x, int y) v, Action<int, int> action)
		{
			Do(v.x, v.y, action);
		}

		public static void Do(int x, int y, int z, Action<int, int, int> action)
		{
			for (var i = 0; i < x; i++) {
				for (var j = 0; j < y; j++) {
					for (var k = 0; k < z; k++) {
						action(i, j, k);
					}
				}
			}
		}
		
		public static void Do(this (int x, int y, int z) v, Action<int, int, int> action)
		{
			Do(v.x, v.y, v.z, action);
		}
	}
}