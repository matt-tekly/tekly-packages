using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tekly.Content
{
    public class ContentProvider : Singleton<ContentProvider, IContentProvider>, IContentProvider
    {
        public async Task<Result> InitializeAsync()
        {
            var operation = Addressables.InitializeAsync();
            
            try {
                await operation.Task;
                return Result.Okay();
            } catch (Exception exception) {
                return Result.Fail(exception.Message);
            }
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