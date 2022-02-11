// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class Binder : MonoBehaviour
    {
        public virtual void Bind(BinderContainer container)
        {
            
        }

        public virtual string ResolveFullKey(string key)
        {
            var container = GetComponentInParent<BinderContainer>();
            
            if (container == null) {
                return key;
            }

            return container.ResolveFullKey(key);
        }
    }
}