using Tekly.Injectors;
using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
    public abstract class ScriptableBinding : ScriptableObject
    {
        public abstract void Bind(InjectorContainer container);
    }
}