using Tekly.Favorites.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class FavoritesPopup : EditorWindow
    {
        private static FavoritesPopup s_instance;
        private bool m_moveToMouse;

        private const int Width = 250;
        private const int Height = 190;
        
        public static void Present()
        {
            if (s_instance != null) {
                s_instance.Close();
                return;
            }

            s_instance = CreateInstance<FavoritesPopup>();
            var texture = CommonUtils.Texture("Editor/Core/Assets/heart.png");
            s_instance.titleContent = new GUIContent("Favorites", texture);
            
            int x = (Screen.currentResolution.width - Width) / 2;
            int y = (Screen.currentResolution.height - Height) / 2;

            s_instance.position = new Rect(x, y, Width, Height);
            s_instance.ShowPopup();
            s_instance.Focus();
        }

        public static void Hide()
        {
            if (s_instance != null) {
                s_instance.Close();
            }
        }

        private void OnEnable()
        {
            var xmlAsset = CommonUtils.Uxml("Editor/Core/FavoritesWindow.uxml");
            xmlAsset.CloneTree(rootVisualElement);
            rootVisualElement.viewDataKey = "tekly/favoriteswindow";

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            Focus();

            rootVisualElement.focusable = true;

            m_moveToMouse = true;
        }
        
        private void OnGUI()
        {
            if (m_moveToMouse && Event.current != null) {
                var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                var x = mousePos.x;
                var y = mousePos.y;
                
                position = new Rect(x, y, Width, Height);

                m_moveToMouse = false;
            }
        }

        private void OnFocus()
        {
            rootVisualElement.Focus();
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (s_instance == this) {
                s_instance = null;
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is >= KeyCode.Alpha0 and <= KeyCode.Alpha9) {
                evt.StopPropagation();
                if (FavoritesData.Instance.HandleShortcut(evt.keyCode, evt.shiftKey)) {
                    Close();
                }
            }

            if (evt.keyCode == KeyCode.Escape) {
                evt.StopPropagation();
                Close();
            }
        }
    }
}