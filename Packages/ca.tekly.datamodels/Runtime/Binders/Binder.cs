// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public abstract class Binder : MonoBehaviour
    {
        public virtual void Bind(BinderContainer container)
        {
            
        }

        public virtual string ResolveFullKey(string key)
        {
            var container = GetComponentInParent<BinderContainer>();
            return container == null ? key : container.ResolveFullKey(key);
        }
    }
}