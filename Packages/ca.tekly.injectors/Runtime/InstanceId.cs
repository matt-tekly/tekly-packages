// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;

namespace Tekly.Injectors
{
    public readonly struct InstanceId : IEquatable<InstanceId>
    {
        public readonly Type Type;
        public readonly string Id;

        public InstanceId(Type type, string id)
        {
            Type = type;
            Id = id;
        }
        
        public bool Equals(InstanceId other)
        {
            return ReferenceEquals(Type, other.Type) && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is InstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            }
        }
    }
}