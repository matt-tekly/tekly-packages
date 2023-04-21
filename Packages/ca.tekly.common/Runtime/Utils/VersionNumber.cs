using System;

namespace Tekly.Common.Utils
{
	/// <summary>
	/// Represents a SemVer version number
	/// </summary>
	[Serializable]
	public class VersionNumber : IComparable<VersionNumber>, IEquatable<VersionNumber>
	{
		public int Major;
		public int Minor;
		public int Patch;

		public int CompareTo(VersionNumber other)
		{
			if (ReferenceEquals(this, other)) {
				return 0;
			}

			if (ReferenceEquals(null, other)) {
				return 1;
			}

			var majorComparison = Major.CompareTo(other.Major);
			if (majorComparison != 0) {
				return majorComparison;
			}

			var minorComparison = Minor.CompareTo(other.Minor);
			if (minorComparison != 0) {
				return minorComparison;
			}

			return Patch.CompareTo(other.Patch);
		}

		public static bool TryParse(string versionString, out VersionNumber version)
		{
			version = new VersionNumber();
			
			var segments = versionString.Split('.');

			if (segments.Length == 0) {
				return int.TryParse(versionString, out version.Major);
			}

			if (segments.Length > 2) {
				if (!int.TryParse(segments[2], out version.Patch)) {
					return false;
				}
			}
			
			if (segments.Length > 1) {
				if (!int.TryParse(segments[1], out version.Minor)) {
					return false;
				}
			}
			
			if (segments.Length > 0) {
				if (!int.TryParse(segments[0], out version.Major)) {
					return false;
				}
			}

			return true;
		}
		
		public bool Equals(VersionNumber other)
		{
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			return obj.GetType() == GetType() && Equals((VersionNumber) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Major, Minor, Patch);
		}

		public static bool operator ==(VersionNumber left, VersionNumber right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(VersionNumber left, VersionNumber right)
		{
			return !Equals(left, right);
		}
		
		public static bool operator <(VersionNumber left, VersionNumber right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(VersionNumber left, VersionNumber right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(VersionNumber left, VersionNumber right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(VersionNumber left, VersionNumber right)
		{
			return left.CompareTo(right) >= 0;
		}
	}
}