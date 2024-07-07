#if UNITY_EDITOR
#define TINKER_ENABLED
#endif

using System;

#if TINKER_ENABLED 
using BaseAttribute = UnityEngine.Scripting.PreserveAttribute;
#else
using BaseAttribute = System.Attribute;
#endif

namespace Tekly.Tinker.Core
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, Inherited = false)]
	public class TinkerPreserveAttribute : BaseAttribute { }
}