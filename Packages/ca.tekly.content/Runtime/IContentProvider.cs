using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tekly.Content
{
    public interface IContentProvider
    {
        Task<Result> InitializeAsync();
        
        IContentOperation<TObject> LoadAssetAsync<TObject>(string key) where TObject : Object;
        IContentOperation<TObject> LoadAssetAsync<TObject>(string key, string label) where TObject : Object;
        IContentOperation<IList<TObject>> LoadAssetsAsync<TObject>(string key) where TObject : Object;
    }
}