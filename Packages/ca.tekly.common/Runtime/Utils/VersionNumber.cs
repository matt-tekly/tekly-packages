using System;

namespace Tekly.Common.Utils
{
	/// <summary>
	/// Represents a SemVer version number
	/// </summary>
	[Serializable]
	public class VersionNumber : IComparable<VersionNumber>
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
	}
}