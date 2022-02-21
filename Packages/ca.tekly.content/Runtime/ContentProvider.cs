using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tekly.Content
{
    public class ContentProvider : Singleton<ContentProvider, IContentProvider>, IContentProvider
    {
        public AsyncOperationHandle<IResourceLocator> InitializeAsync()
        {
            return Addressables.InitializeAsync();
        }

        public IContentOperation<TObject> LoadAssetAsync<TObject>(string key) where TObject : Object
        {
            var handle = Addressables.LoadAssetAsync<TObject>(key);
            return new ContentOperation<TObject>(handle);
        }
        
        public IContentOperation<TObject> LoadAssetAsync<TObject>(string key, string label) where TObject : Object
        {
            IEnumerable<string> keys = new[] { key, label };
            var handle = Addressables.LoadAssetsAsync<TObject>(keys, null, Addressables.MergeMode.Intersection);
            
            return new SingleContentOperation<TObject>(handle);
        }
        
        public IContentOperation<IList<TObject>> LoadAssetsAsync<TObject>(string key) where TObject : Object
        {
            var handle = LoadAssetsAsync<TObject>(new []{key});
            return new MultiContentOperation<TObject>(handle);
        }

        private AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IEnumerable<string> keys)
        {
            return Addressables.LoadAssetsAsync<TObject>(keys, null, Addressables.MergeMode.Union);
        }
    }
}