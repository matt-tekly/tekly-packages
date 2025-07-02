using System;
using UnityEngine.AddressableAssets;

namespace Tekly.Lofi.Core
{
	[Serializable]
	public class LofiClipBankDefinitionRef : AssetReferenceT<LofiClipBankDefinition>, IEquatable<LofiClipBankDefinitionRef>
	{
		public LofiClipBankDefinitionRef(string guid) : base(guid) {}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj is null || obj.GetType() != GetType()) {
				return false;
			}

			return Equals((LofiClipBankDefinitionRef)obj);
		}

		public bool Equals(LofiClipBankDefinitionRef other)
		{
			if (ReferenceEquals(this, other)) {
				return true;
			}

			if (other is null) {
				return false;
			}

			return AssetGUID == other.AssetGUID;
		}

		public override int GetHashCode()
		{
			return AssetGUID?.GetHashCode() ?? 0;
		}

		public static bool operator ==(LofiClipBankDefinitionRef left, LofiClipBankDefinitionRef right)
		{
			if (ReferenceEquals(left, right)) {
				return true;
			}

			if (left is null || right is null) {
				return false;
			}

			return left.Equals(right);
		}

		public static bool operator !=(LofiClipBankDefinitionRef left, LofiClipBankDefinitionRef right)
		{
			return !(left == right);
		}
	}
}