using UnityEngine;

namespace Tekly.Injectors.Utils
{
	public abstract class InjectableBehaviour : MonoBehaviour
	{
		public virtual void Initialize() { }
		public virtual void PostInitialize() { }
	}
}