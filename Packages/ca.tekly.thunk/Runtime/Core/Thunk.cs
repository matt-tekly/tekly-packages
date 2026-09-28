using System;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Thunk.Music;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Tekly.Thunk.Core
{
    public class Thunk : Singleton<Thunk>, IDisposable
    {
        public const int INVALID_ID = -1;
        
        /// <summary>
        /// Tracks when Unity itself is paused, which can pause all audio
        /// </summary>
        public bool Paused { get; private set; }
        
        public ThunkClipStateManager ClipStateManager { get; } = new ThunkClipStateManager();

        public ThunkTrackManager TrackManager => m_trackManager ??= new ThunkTrackManager();
        
        /// <summary>
        /// Emitter for one-shot gameplay sounds. These pause with AudioListener.pause.
        /// </summary>
        public ThunkEmitter OneShot => GetOrCreateEmitter(ref m_oneShotEmitter, "[Thunk] OneShot", false);
        
        /// <summary>
        /// Emitter for UI sounds. These play through AudioListener.pause and fade on unscaled time,
        /// so they work in pause menus.
        /// </summary>
        public ThunkEmitter UiSounds => GetOrCreateEmitter(ref m_uiSoundsEmitter, "[Thunk] UI Sounds", true);
        
        internal int NextClipStateId;
        
        private ThunkTrackManager m_trackManager;
        private ThunkEmitter m_oneShotEmitter;
        private ThunkEmitter m_uiSoundsEmitter;
        
        public Thunk()
        {
            LifeCycle.Instance.Pause += OnPause;
            
            LifeCycle.Instance.Update += () => {
                ClipStateManager.Tick(Time.deltaTime, Time.unscaledDeltaTime);
            };
        }
        
        private void OnPause(bool paused)
        {
            Paused = paused;
        }

        public void Dispose()
        {
            ClipStateManager.Dispose();
        }
        
        public static void SetVolume(AudioMixer mixer, string id, double linearValue)
        {
            mixer.SetFloat(id, ToDecibel((float)linearValue));
        }
		
        public static float GetVolume(AudioMixer mixer, string id)
        {
            mixer.GetFloat(id, out var volume);
            return ToLinear(volume);
        }
        
        private static ThunkEmitter GetOrCreateEmitter(ref ThunkEmitter emitter, string name, bool playsWhilePaused)
        {
            // Unity null check, so a destroyed emitter is recreated
            if (emitter == null) {
                var go = new GameObject(name);
                Object.DontDestroyOnLoad(go);
                
                emitter = go.AddComponent<ThunkEmitter>();
                emitter.IgnoreListenerPause = playsWhilePaused;
                emitter.UseUnscaledDeltaTime = playsWhilePaused;
            }

            return emitter;
        }
        
        public static float ToDecibel(float linear)
        {
            return linear > 0 ? 20.0f * Mathf.Log10(linear) : -144.0f;
        }

        public static float ToLinear(float decibel)
        {
            return Mathf.Pow(10.0f, decibel / 20.0f);
        }
    }
}
