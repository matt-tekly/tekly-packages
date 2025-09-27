using System;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.TreeState
{
    /// <summary>
    /// The base class for TreeState and TreeStateActivity.
    /// TreeActivity is updated and initialized by the TreeStateManager at it's root.
    /// </summary>
    public class TreeActivity : MonoBehaviour
    {
        public virtual string Name => name;
        public virtual string FullName => Name;
        public string TypeName => m_typeName ??= GetType().Name;
        
        public ActivityMode Mode
        {
            get => m_mode;
            private set {
                m_mode = value;
                m_monitor?.ActivityModeChanged(this);
            }
        }

        private string m_typeName;
        private IActivityMonitor m_monitor;
        private ActivityMode m_mode;
        internal readonly Disposables Disposables = new Disposables();

        public void SetMonitor(IActivityMonitor monitor)
        {
            m_monitor = monitor;
        }

        /// <summary>
        /// Called by the TreeStateManager every frame.
        /// </summary>
        public void ActivityUpdate()
        {
            switch (Mode) {
                case ActivityMode.Inactive:
                    break;
                case ActivityMode.Loading:
                    LoadingUpdate();

                    if (IsDoneLoading()) {
                        Mode = ActivityMode.ReadyToActivate;
                    }

                    break;
                case ActivityMode.ReadyToActivate:
                    ReadyToActivateUpdate();
                    break;
                case ActivityMode.Active:
                    ActiveUpdate();
                    break;
                case ActivityMode.Unloading:
                    UnloadingUpdate();

                    if (IsDoneUnloading()) {
                        Deactivate();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Load()
        {
            Mode = ActivityMode.Loading;
            PreLoad();
            LoadingStarted();
        }

        public void Activate()
        {
            Mode = ActivityMode.Active;
            ActiveStarted();
        }

        public void Unload()
        {
            Mode = ActivityMode.Unloading;
            UnloadingStarted();
        }

        private void Deactivate()
        {
            Mode = ActivityMode.Inactive;
            InactiveStarted();
            PostInactive();
            Disposables.Clear();
        }

        protected virtual void PreLoad() { }
        protected virtual void LoadingStarted() { }

        protected virtual bool IsDoneLoading()
        {
            return true;
        }

        protected virtual void LoadingUpdate() { }

        protected virtual void ReadyToActivateUpdate() { }

        protected virtual void ActiveStarted() { }

        protected virtual void ActiveUpdate() { }

        protected virtual void UnloadingStarted() { }

        protected virtual bool IsDoneUnloading()
        {
            return true;
        }

        protected virtual void UnloadingUpdate() { }

        protected virtual void InactiveStarted() { }
        protected virtual void PostInactive() { }

        protected virtual void OnApplicationQuit() { }

        protected virtual void OnDestroy()
        {
            Disposables.Clear();
        }
    }

    public static class TreeActivityDisposableExtensions
    {
        public static IDisposable AddTo(this IDisposable disposable, TreeActivity treeActivity)
        {
            treeActivity.Disposables.Add(disposable);
            return disposable;
        }
    }
}