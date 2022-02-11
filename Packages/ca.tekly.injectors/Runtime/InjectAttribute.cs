// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Tekly.Injectors
{
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectAttribute : PreserveAttribute
    {
        public readonly string Id;

        public InjectAttribute(string id = null)
        {
            Id = id;
        }
    }
}