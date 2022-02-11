using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Tekly.Common.Utils
{
    public interface ILoadable : IDisposable
    {
        bool IsLoading { get; }
        void Load();
    }

    public class Loadables
    {
        private readonly List<ILoadable> m_loadables = new List<ILoadable>();
        private bool m_loadStarted;
        
        public void Add(ILoadable loadable)
        {
            Assert.IsFalse(m_loadStarted);
            
            m_loadables.Add(loadable);
        }

        public void Load()
        {
            Assert.IsFalse(m_loadStarted);
            m_loadStarted = true;
            
            foreach (var loadable in m_loadables) {
                loadable.Load();
            }
        }

        public void Dispose()
        {
            foreach (var loadable in m_loadables) {
                loadable.Dispose();
            }
            
            m_loadables.Clear();
            m_loadStarted = false;
        }

        public bool IsLoading()
        {
            foreach (var loadable in m_loadables) {
                if (loadable.IsLoading) {
                    return true;
                }
            }

            return false;
        }
    }
}