using System.Threading.Tasks;
using Tekly.Common.Utils;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tekly.Extensions.Addressables
{
	public static class AddressablesExtensions
	{
		private static async Task<GameObject> InstantiateInjectAsync(this AssetReferenceGameObject assetReference, InjectorContainer container)
		{
			var go = await assetReference.LoadAssetAsync().Task;
			
			go = PrefabProtector.Protect(go);
			go = Object.Instantiate(go);
			
			go.GetComponent<HierarchyInjector>().Inject(container);
			go.SetActive(true);

			return go;
		}
	}
}