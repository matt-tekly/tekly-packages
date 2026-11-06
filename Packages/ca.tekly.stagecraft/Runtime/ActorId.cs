using System;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// Identifies one spawn of an actor: a registry slot plus the generation that slot had when the actor was
	/// activated. Once the actor despawns the slot's generation moves on, so every copy of this id resolves to
	/// nothing from then on. <c>default</c> is <see cref="None"/>. Local to one machine, never a network identity.
	/// </summary>
	public readonly struct ActorId : IEquatable<ActorId>
	{
		public static readonly ActorId None = default;

		public readonly int Index;
		public readonly uint Generation;

		public bool IsNone => Generation == 0;

		public ActorId(int index, uint generation)
		{
			Index = index;
			Generation = generation;
		}

		/// <summary>
		/// The id as one 64-bit value, for physics user data and network payloads.
		/// </summary>
		public ulong Pack()
		{
			return ((ulong) (uint) Index << 32) | Generation;
		}

		public static ActorId Unpack(ulong packed)
		{
			return new ActorId((int) (packed >> 32), (uint) packed);
		}

		public bool Equals(ActorId other)
		{
			return Index == other.Index && Generation == other.Generation;
		}

		public override bool Equals(object obj)
		{
			return obj is ActorId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (Index * 397) ^ (int) Generation;
		}

		public static bool operator ==(ActorId a, ActorId b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(ActorId a, ActorId b)
		{
			return !a.Equals(b);
		}

		public override string ToString()
		{
			return IsNone ? "ActorId.None" : $"ActorId({Index}:{Generation})";
		}
	}
}
