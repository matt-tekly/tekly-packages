using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Tekly.Content
{
    public interface IContentProvider
    {
        Task<Result> InitializeAsync();
        Task<Result> UpdateRemoteCatalogAsync();
        
        IContentOperation<TObject> LoadAssetAsync<TObject>(string key) where TObject : Object;
        IContentOperation<TObject> LoadAssetAsync<TObject>(string key, string label) where TObject : Object;
        IContentOperation<IList<TObject>> LoadAssetsAsync<TObject>(string key) where TObject : Object;

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string scene, LoadSceneMode mode, bool activateOnLoad = true);
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle);
    }
}