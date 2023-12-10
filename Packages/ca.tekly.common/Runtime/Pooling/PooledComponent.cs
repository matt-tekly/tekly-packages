using UnityEngine;

namespace Tekly.Common.Pooling
{
	public class PooledComponent<T> : MonoBehaviour where T : MonoBehaviour 
	{
		public ComponentPool<T> Pool { get; set; }
		public int PoolId { get; set; }
        
		public string Name
		{
			get => gameObject.name;
			set => gameObject.name = value;
		}
        
		public virtual PooledComponent<T> Instantiate()
		{
			return Instantiate(this);
		}

		public void Return()
		{
			gameObject.SetActive(false);
			OnReturned();
			Pool.Return(this);
		}

		protected virtual void OnReturned()
		{
            
		}
	}
}