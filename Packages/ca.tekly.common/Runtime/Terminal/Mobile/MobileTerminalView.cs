using System.Collections;
using System.Linq;
using Tekly.Common.Terminal.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Terminal.Mobile
{
    public class MobileTerminalView : TerminalView
    {
        public InputField Messages;

        public CanvasScaler Scaler;
        public RectTransform Transform;

        private TerminalRoot m_container;
        private CommandStore m_commandStore;

        private TouchScreenKeyboard m_keyboard;
        private Coroutine m_poll;

        public override float GetScale()
        {
            return Scaler.scaleFactor;
        }

        public override void SetScale(float scale)
        {
            Scaler.scaleFactor = scale;
        }

        public override float GetSize()
        {
            return 1f - Transform.anchorMin.y;
        }

        public override void SetSize(float size)
        {
            Transform.anchorMin = new Vector2(Transform.anchorMin.x, 1f - size);
        }

        public override void Initialize(TerminalRoot container, CommandStore commandStore)
        {
            m_container = container;
            m_commandStore = commandStore;
            m_commandStore.MessagesChanged += OnMessagesChanged;
        }

        private void Awake()
        {
            gameObject.SetActive(false);
            Messages.text = $"Enter {"help".Gray()} for information.";
        }

        private void OnEnable()
        {
            OpenKeyboard();
            m_poll = StartCoroutine(MonitorKeyboard());
        }

        private void OnDisable()
        {
            CloseKeyboard();
        }

        public void Close()
        {
            m_container.SetActive(false);
        }

        public void ToggleKeyboard()
        {
            if (m_keyboard == null) {
                OpenKeyboard();
                m_poll = StartCoroutine(MonitorKeyboard());
            } else {
                CloseKeyboard();
            }
        }

        private IEnumerator MonitorKeyboard()
        {
            yield return new WaitForSeconds(0.5f);

            while (isActiveAndEnabled) {
                var text = m_keyboard.text;

                if (!string.IsNullOrEmpty(text)) {
                    var lastCharacter = text[text.Length - 1];

                    if (lastCharacter == '\n') {
                        Debug.Log("Newline detected");

                        m_commandStore.Execute(text.Substring(0, text.Length - 1));
                        m_keyboard.text = "";
                    }
                }

                if (m_keyboard.status != TouchScreenKeyboard.Status.Visible) {
                    m_keyboard = null;
                    Transform.offsetMin = new Vector2(Transform.offsetMin.x, 0);
                    break;
                }

                Transform.offsetMin = new Vector2(Transform.offsetMin.x, GetKeyboardHeight(true));
                yield return null;
            }
        }

        private void OnMessagesChanged()
        {
            Messages.text = string.Join("\n", m_commandStore.Messages.Select(x => x.ToConsoleMessage()));
            Messages.textComponent.text = Messages.text;
        }

        private void OpenKeyboard()
        {
            m_keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, true);
        }
        
        private void CloseKeyboard()
        {
            if (m_poll != null) {
                StopCoroutine(m_poll);
                m_poll = null;
            }

            if (m_keyboard != null) {
                m_keyboard.active = false;
                m_keyboard = null;
            }
        }

        private static int GetKeyboardHeight(bool includeInput)
        {
#if UNITY_EDITOR
            return 0;
#elif UNITY_ANDROID
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject unityPlayer =
 unityClass.GetStatic<AndroidJavaObject>("currentActivity").Get<AndroidJavaObject>("mUnityPlayer");
            AndroidJavaObject view = unityPlayer.Call<AndroidJavaObject>("getView");
            AndroidJavaObject dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");
            if (view == null || dialog == null)
                return 0;
            var decorHeight = 0;
            if (includeInput)
            {
                AndroidJavaObject decorView =
 dialog.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
                if (decorView != null)
                    decorHeight = decorView.Call<int>("getHeight");
            }
            using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
            {
                view.Call("getWindowVisibleDisplayFrame", rect);
                return Screen.height - rect.Call<int>("height") + decorHeight;
            }
        }
#elif UNITY_IOS
        return (int)TouchScreenKeyboard.area.height;
#endif
        }
    }
}