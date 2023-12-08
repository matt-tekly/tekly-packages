using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tekly.Content
{
    public interface IContentOperation<T>
    {
        Task<T> Task { get; }
        public bool HasError { get; }
        bool IsDone { get; }
        T Result { get; }
        Exception Exception { get; }
        event Action<IContentOperation<T>> Completed;
        TaskAwaiter<T> GetAwaiter();
        
        void Release();
    }
    
    public class ContentOperation<T> : IContentOperation<T> where T : Object
    {
        public Task<T> Task => m_task ??= GetTask();
        public bool HasError => m_handle.Status == AsyncOperationStatus.Failed;
        public bool IsDone => m_handle.IsDone;
        public T Result => m_handle.Result;
        public Exception Exception => m_handle.OperationException;

        public event Action<IContentOperation<T>> Completed;

        private Task<T> m_task;
        private AsyncOperationHandle<T> m_handle;

        public ContentOperation(AsyncOperationHandle<T> handle)
        {
            m_handle = handle;
            m_handle.Completed += OnCompleted;
        }

        private void OnCompleted(AsyncOperationHandle<T> obj)
        {
            Completed?.Invoke(this);
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        public void Release()
        {
            m_handle.Completed -= OnCompleted;
            Addressables.Release(m_handle);
        }

        private Task<T> GetTask()
        {
            return m_handle.Task;
        }
    }
}