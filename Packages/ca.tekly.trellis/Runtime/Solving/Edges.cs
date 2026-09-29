using System;

namespace Tekly.Trellis
{
	/// <summary>
	/// Space on each side of a rect, used for padding and margins. A value type (unlike RectOffset),
	/// so assigning it through a property always marks the layout dirty. Supports fractional values.
	/// </summary>
	[Serializable]
	public struct Edges : IEquatable<Edges>
	{
		public float Left;
		public float Right;
		public float Top;
		public float Bottom;

		public Edges(float left, float right, float top, float bottom)
		{
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
		}

		public static Edges All(float value)
		{
			return new Edges(value, value, value, value);
		}

		public static Edges Symmetric(float horizontal, float vertical)
		{
			return new Edges(horizontal, horizontal, vertical, vertical);
		}

		public float Horizontal => Left + Right;
		public float Vertical => Top + Bottom;

		/// <summary>
		/// Left or top: the side positions are measured from. Axis 0 is horizontal, 1 is vertical.
		/// </summary>
		public float Start(int axis)
		{
			return axis == 0 ? Left : Top;
		}

		/// <summary>
		/// Right or bottom. Axis 0 is horizontal, 1 is vertical.
		/// </summary>
		public float End(int axis)
		{
			return axis == 0 ? Right : Bottom;
		}

		public float Total(int axis)
		{
			return axis == 0 ? Horizontal : Vertical;
		}

		public bool Equals(Edges other)
		{
			return Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;
		}

		public override bool Equals(object obj)
		{
			return obj is Edges other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked {
				var hash = Left.GetHashCode();
				hash = (hash * 397) ^ Right.GetHashCode();
				hash = (hash * 397) ^ Top.GetHashCode();
				hash = (hash * 397) ^ Bottom.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(Edges a, Edges b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Edges a, Edges b)
		{
			return !a.Equals(b);
		}

		public override string ToString()
		{
			return $"(L {Left}, R {Right}, T {Top}, B {Bottom})";
		}
	}
}
