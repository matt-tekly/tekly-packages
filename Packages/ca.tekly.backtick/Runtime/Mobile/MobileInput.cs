using System.Collections;
using UnityEngine;

namespace Tekly.Backtick.Mobile
{
    public class MobileInput : MonoBehaviour
    {
        private TouchScreenKeyboard m_keyboard;
        private Coroutine m_poll;
        
        private void OnEnable()
        {
            m_keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false);
            m_poll = StartCoroutine(MonitorKeyboard());
        }

        private void OnDisable()
        {
            StopCoroutine(m_poll);
        }

        private IEnumerator MonitorKeyboard()
        {
            yield return 0.5f;

            if (m_keyboard.status == TouchScreenKeyboard.Status.Done) {
                Debug.Log("Done");
            }
        }
    }
}