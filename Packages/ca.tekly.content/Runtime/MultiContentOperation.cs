using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tekly.Content
{
    public class MultiContentOperation<T> : IContentOperation<IList<T>> where T : Object
    {
        public Task<IList<T>> Task => m_task ??= m_handle.Task;
        public bool HasError => m_handle.Status == AsyncOperationStatus.Failed;
        public bool IsDone => m_handle.IsDone;
        public IList<T> Result => m_handle.Result;
        
        private Task<IList<T>> m_task;
        private readonly AsyncOperationHandle<IList<T>> m_handle;

        public MultiContentOperation(AsyncOperationHandle<IList<T>> handle)
        {
            m_handle = handle;
        }
        
        public TaskAwaiter<IList<T>> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        public void Release()
        {
            Addressables.Release(m_handle);
        }
    }
}