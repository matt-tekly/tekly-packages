using System;

namespace Tekly.Common.Utils
{
	[Serializable]
	public struct OptionalBool
	{
		public bool IsSet;
		public bool Value;
	}
	
	[Serializable]
	public struct OptionalInt
	{
		public bool IsSet;
		public int Value;
	}
	
	[Serializable]
	public struct OptionalFloat
	{
		public bool IsSet;
		public float Value;
	}
	
	[Serializable]
	public struct OptionalDouble
	{
		public bool IsSet;
		public double Value;
	}
	
	[Serializable]
	public struct OptionalString
	{
		public bool IsSet;
		public string Value;
	}
}