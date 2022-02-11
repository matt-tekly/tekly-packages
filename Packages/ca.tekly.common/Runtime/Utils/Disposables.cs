// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;

namespace Tekly.Common.Utils
{
    public static class Disposables
    {
        public static readonly IDisposable Empty = new EmptyDisposable();
        
        public static IDisposable AddTo(this IDisposable disposable, DisposableList disposables)
        {
            disposables.Add(disposable);
            return disposable;
        }
    }

    public class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }

    public class DisposableList : IDisposable
    {
        private readonly List<IDisposable> m_disposables = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            m_disposables.Add(disposable);
        }

        public void Remove(IDisposable disposable)
        {
            m_disposables.Remove(disposable);
        }

        public void Clear()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach (var disposable in m_disposables) {
                disposable.Dispose();
            }
            
            m_disposables.Clear();
        }
    }
}