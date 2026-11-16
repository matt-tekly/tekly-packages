using UnityEditor;
using UnityEngine;

namespace Tekly.Wipes
{
    [CustomEditor(typeof(WipeLook))]
    public class WipeLookEditor : Editor
    {
        private WipePreview m_preview;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            WipeStyleGUI.Draw(serializedObject.FindProperty("m_style"), m_preview);

            if (serializedObject.ApplyModifiedProperties()) {
                WipeEditorUtils.RefreshWipes((WipeLook) target);
            }
        }

        private void OnEnable()
        {
            m_preview = new WipePreview(this);
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            m_preview?.Dispose();
            m_preview = null;
        }

        private void OnUndoRedo()
        {
            if (target != null) {
                WipeEditorUtils.RefreshWipes((WipeLook) target);
            }
        }
    }

    [CustomEditor(typeof(Wipe))]
    [CanEditMultipleObjects]
    public class WipeEditor : Editor
    {
        private WipePreview m_preview;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"), new GUIContent("Block Raycasts", "Blocks input under the wipe while anything is covered"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_focus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_trim"));

            EditorGUILayout.Space();
            WipeEditorUtils.DrawLook(serializedObject, m_preview);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_preview = new WipePreview(this);
        }

        private void OnDisable()
        {
            m_preview?.Dispose();
            m_preview = null;
        }
    }

    [CustomEditor(typeof(WipeReveal))]
    [CanEditMultipleObjects]
    public class WipeRevealEditor : Editor
    {
        private WipePreview m_preview;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!serializedObject.isEditingMultipleObjects) {
                var canvas = ((WipeReveal) target).GetComponent<Canvas>();

                if (canvas != null && (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)) {
                    EditorGUILayout.HelpBox("A Wipe Reveal needs a screen space root canvas. Unity can only send a root canvas to a texture.", MessageType.Warning);
                }
            }

            EditorGUILayout.HelpBox("Owns the canvas's enabled flag. While a transition is part way it also owns the canvas's render mode, camera and plane distance, and puts them back when it ends.", MessageType.None);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_coverage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_focus"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_textureScale"));

            EditorGUILayout.Space();
            WipeEditorUtils.DrawLook(serializedObject, m_preview);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_preview = new WipePreview(this);
        }

        private void OnDisable()
        {
            m_preview?.Dispose();
            m_preview = null;
        }
    }

    internal static class WipeEditorUtils
    {
        /// <summary>
        /// The shared look with a preview of it, or the inline style when there is none.
        /// </summary>
        public static void DrawLook(SerializedObject serializedObject, WipePreview preview)
        {
            var look = serializedObject.FindProperty("m_look");
            EditorGUILayout.PropertyField(look);

            if (look.hasMultipleDifferentValues) {
                return;
            }

            if (look.objectReferenceValue == null) {
                WipeStyleGUI.Draw(serializedObject.FindProperty("m_style"), preview);
                return;
            }

            var style = ((WipeLook) look.objectReferenceValue).Style;
            var warning = style.GetWarning();

            if (warning != null) {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            preview?.Draw(style);
        }

        /// <summary>
        /// Redraws every loaded Wipe that draws with a look, after the look was edited. A Wipe only pushes
        /// its style to its material when told to, and editing an asset tells it nothing.
        /// </summary>
        public static void RefreshWipes(WipeLook look)
        {
            foreach (var wipe in Resources.FindObjectsOfTypeAll<Wipe>()) {
                if (wipe.Look == look && wipe.gameObject.scene.IsValid()) {
                    wipe.Refresh();
                }
            }

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
