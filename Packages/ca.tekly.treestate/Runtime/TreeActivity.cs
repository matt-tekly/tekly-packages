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

        /// <summary>
        /// Seconds a load or unload can take before it is reported as slow.
        /// 0 uses the monitor's SlowThreshold, negative never reports.
        /// </summary>
        public float SlowThreshold { get; set; }
        
        public ActivityMode Mode
        {
            get => m_mode;
            private set {
                var previousMode = m_mode;
                m_mode = value;
                m_modeStartTime = Time.unscaledTime;
                m_slowReported = false;
                m_monitor?.ActivityModeChanged(this, previousMode);
            }
        }
        
        [NonSerialized] private string m_typeName;
        private IActivityMonitor m_monitor;
        private ActivityMode m_mode;
        private float m_modeStartTime;
        private bool m_slowReported;
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
                        CheckSlow(true);
                        Mode = ActivityMode.ReadyToActivate;
                    } else {
                        CheckSlow(false);
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
                        CheckSlow(true);
                        Deactivate();
                    } else {
                        CheckSlow(false);
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

        /// <summary>
        /// Reports the current load or unload to the monitor if it has run past SlowThreshold, or the monitor's
        /// SlowThreshold when this activity doesn't have its own.
        /// While still running it reports once. On completion it reports again with the final duration.
        /// </summary>
        private void CheckSlow(bool completed)
        {
            if (m_monitor == null || (m_slowReported && !completed)) {
                return;
            }

            var threshold = SlowThreshold != 0 ? SlowThreshold : m_monitor.SlowThreshold;
            if (threshold <= 0) {
                return;
            }

            // unscaledTime doesn't advance while the Editor is paused, so pauses aren't reported as slow loads
            var duration = Time.unscaledTime - m_modeStartTime;
            if (duration > threshold) {
                m_slowReported = true;
                m_monitor.ActivitySlow(this, duration, completed);
            }
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
            m_monitor = null;
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