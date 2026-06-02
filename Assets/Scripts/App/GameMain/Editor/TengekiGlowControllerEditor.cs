using System;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    [CustomEditor(typeof(TengekiGlowController))]
    public sealed class TengekiGlowControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("発光確認", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPreviewButton("通常", c => c.ShowNormal());
                DrawPreviewButton("赤", c => c.SetRed());
                DrawPreviewButton("青", c => c.SetBlue());
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPreviewButton("黄", c => c.SetYellow());
                DrawPreviewButton("緑", c => c.SetGreen());
                DrawPreviewButton("虹", c => c.SetRainbow());
            }
        }

        private void DrawPreviewButton(string label, Action<TengekiGlowController> action)
        {
            if (GUILayout.Button(label, GUILayout.Height(28)))
                ApplyPreview(action, $"Preview Tengeki Glow {label}");
        }

        private void ApplyPreview(Action<TengekiGlowController> action, string undoName)
        {
            foreach (var item in targets)
            {
                if (item is not TengekiGlowController controller)
                    continue;

                Undo.RecordObject(controller, undoName);
                RecordTargetRenderers(controller, undoName);

                action(controller);

                EditorUtility.SetDirty(controller);
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            }
        }

        private static void RecordTargetRenderers(TengekiGlowController controller, string undoName)
        {
            var serializedController = new SerializedObject(controller);
            var targetsProperty = serializedController.FindProperty("_targets");
            if (targetsProperty == null || !targetsProperty.isArray)
                return;

            for (var i = 0; i < targetsProperty.arraySize; i++)
            {
                var renderer = targetsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (renderer == null)
                    continue;

                Undo.RecordObject(renderer, undoName);
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
    }
}
