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
                var previousMode = m_mode;
                m_mode = value;
                m_monitor?.ActivityModeChanged(this, previousMode);
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
            // If we're loading, active, or ready then we're already loading or loaded and don't need to start loading
            if (m_mode == ActivityMode.Loading || m_mode == ActivityMode.Active || m_mode == ActivityMode.ReadyToActivate) {
                return;
            }
            
            Mode = ActivityMode.Loading;
            PreLoad();
            LoadingStarted();
        }

        public void Activate()
        {
            if (m_mode == ActivityMode.Active) {
                return;
            }
            
            Mode = ActivityMode.Active;
            ActiveStarted();
        }

        public void Unload()
        {
            // If we're unloading or inactive then we're already unloading or unloaded and don't need to start unloading
            if (m_mode == ActivityMode.Unloading || m_mode == ActivityMode.Inactive) {
                return;
            }
            
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