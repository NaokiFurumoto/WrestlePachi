#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSys.EditorTools
{
    [CustomEditor(typeof(UIAnimator))]
    public sealed class UIAnimatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("テスト再生", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var animator = (UIAnimator)target;

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("▶ Play", GUILayout.Height(32)))
                        animator.Play();

                    if (GUILayout.Button("■ Stop", GUILayout.Height(32)))
                        animator.Stop();
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("再生中のみ動作します", MessageType.Info);
        }
    }
}
#endif
