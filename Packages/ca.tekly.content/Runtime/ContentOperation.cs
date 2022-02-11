using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tekly.Content
{
    public interface IContentOperation<T>
    {
        Task<T> Task { get; }
        public bool HasError { get; }
        bool IsDone { get; }
        T Result { get; }

        TaskAwaiter<T> GetAwaiter();
        
        void Release();
    }
    
    public class ContentOperation<T> : IContentOperation<T> where T : Object
    {
        public Task<T> Task => m_task ??= GetTask();
        public bool HasError => m_handle.Status == AsyncOperationStatus.Failed;
        public bool IsDone => m_handle.IsDone;
        public T Result => m_handle.Result;

        private Task<T> m_task;
        private readonly AsyncOperationHandle<T> m_handle;

        public ContentOperation(AsyncOperationHandle<T> handle)
        {
            m_handle = handle;
        }
        
        public TaskAwaiter<T> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        public void Release()
        {
            Addressables.Release(m_handle);
        }

        private Task<T> GetTask()
        {
            return m_handle.Task;
        }
    }
}