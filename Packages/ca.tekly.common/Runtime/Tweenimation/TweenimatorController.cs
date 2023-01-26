using System;
using UnityEngine;

namespace Tekly.Common.Tweenimation
{
    [Serializable]
    public struct TweenimatorTrigger
    {
        public string Name;
        public Tweenimator Tweenimator;
    }
    
    public class TweenimatorController : MonoBehaviour
    {
        [SerializeField] private TweenimatorTrigger[] m_triggers;
        
        public void Trigger(string trigger)
        {
            foreach (var tweenimatorTrigger in m_triggers) {
                if (tweenimatorTrigger.Name == trigger) {
                    tweenimatorTrigger.Tweenimator.Play();
                    break;
                }
            }
        }
    }
}