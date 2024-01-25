using System;

namespace Tekly.Simulant.Core
{
	public interface IInit
	{
		void Init();
	}
	
	public interface IRecycle
	{
		void Recycle();
	}
	
	public interface IAutoRecycle {	}
	
	public delegate void InitDelegate<T>(ref T target) where T : struct;
	public delegate void RecycleDelegate<T>(ref T target) where T : struct;
	
	public static class DataInit<T> where T : struct
	{
		public static readonly InitDelegate<T> Init;

		static DataInit()
		{
			if (!typeof(IInit).IsAssignableFrom(typeof(T))) {
				throw new Exception($"Type {typeof(T)} does not derive from {nameof(IInit)}");
			}
			
			Init = Get<InitDelegate<T>>(nameof(Trick.Init));
		}

		private static TDelegate Get<TDelegate>(string name) where TDelegate : Delegate
		{
			var method = typeof(Trick).GetMethod(name).MakeGenericMethod(typeof(T));
			return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), method);
		}

		static class Trick
		{
			public static void Init<U>(ref U target) where U : struct, IInit
			{
				target.Init();
			}
		}
	}
	
	public static class DataRecycle<T> where T : struct
	{
		public static readonly RecycleDelegate<T> Recycle;

		static DataRecycle()
		{
			if (!typeof(IRecycle).IsAssignableFrom(typeof(T))) {
				throw new Exception($"Type {typeof(T)} does not derive from {nameof(IRecycle)}");
			}
			
			Recycle = Get<RecycleDelegate<T>>(nameof(Trick.Recycle));
		}

		private static TDelegate Get<TDelegate>(string name) where TDelegate : Delegate
		{
			var method = typeof(Trick).GetMethod(name).MakeGenericMethod(typeof(T));
			return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), method);
		}

		static class Trick
		{
			public static void Recycle<U>(ref U target) where U : struct, IRecycle
			{
				target.Recycle();
			}
		}
	}
}