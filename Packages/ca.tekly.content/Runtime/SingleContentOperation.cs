using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tekly.Content
{
    public class SingleContentOperation<T> : IContentOperation<T> where T : Object
    {
        public Task<T> Task => m_task ??= GetTask();
        public bool HasError => m_handle.Status == AsyncOperationStatus.Failed;
        public bool IsDone => m_handle.IsDone;
        public T Result
        {
            get {
                if (m_handle.Result != null && m_handle.Result.Count > 0) {
                    return m_handle.Result[0];
                }

                return null;
            }
        }
        
        public Exception Exception => m_handle.OperationException;
        public event Action<IContentOperation<T>> Completed;
        
        private Task<T> m_task;
        private readonly AsyncOperationHandle<IList<T>> m_handle;

        public SingleContentOperation(AsyncOperationHandle<IList<T>> handle)
        {
            m_handle = handle;
            m_handle.Completed += OnCompleted;
        }

        private void OnCompleted(AsyncOperationHandle<IList<T>> obj)
        {
            Completed?.Invoke(this);
        }

        public void Release()
        {
            m_handle.Completed -= OnCompleted;
            Addressables.Release(m_handle);
        }
        
        public TaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        private async Task<T> GetTask()
        {
            var result = await m_handle.Task;

            if (m_handle.Status == AsyncOperationStatus.Failed) {
                return null;
            }

            return result[0];
        }
    }
}