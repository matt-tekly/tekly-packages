using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tekly.Content
{
    public class MultiContentOperation<T> : IContentOperation<IList<T>> where T : Object
    {
        public Task<IList<T>> Task => m_task ??= m_handle.Task;
        public bool HasError => m_handle.Status == AsyncOperationStatus.Failed;
        public bool IsDone => m_handle.IsDone;
        public IList<T> Result => m_handle.Result;
        public Exception Exception => m_handle.OperationException;
        public event Action<IContentOperation<IList<T>>> Completed;
        
        private Task<IList<T>> m_task;
        private readonly AsyncOperationHandle<IList<T>> m_handle;

        public MultiContentOperation(AsyncOperationHandle<IList<T>> handle)
        {
            m_handle = handle;
            m_handle.Completed += OnCompleted;
        }

        private void OnCompleted(AsyncOperationHandle<IList<T>> obj)
        {
            Completed?.Invoke(this);
        }

        public TaskAwaiter<IList<T>> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        public void Release()
        {
            m_handle.Completed -= OnCompleted;
            Addressables.Release(m_handle);
        }
    }
}