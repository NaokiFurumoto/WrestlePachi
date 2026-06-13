#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    [CustomEditor(typeof(ScreenEffectController))]
    public sealed class ScreenEffectControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("テスト再生", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var ctrl = (ScreenEffectController)target;

                if (GUILayout.Button("★ へそ入賞（きらっ）"))
                    ScreenEffectController.PlayHeso();

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Red スキル"))    ScreenEffectController.PlaySkill(HoldType.Red);
                if (GUILayout.Button("Yellow スキル")) ScreenEffectController.PlaySkill(HoldType.Yellow);
                if (GUILayout.Button("Green スキル"))  ScreenEffectController.PlaySkill(HoldType.Green);
                if (GUILayout.Button("Blue スキル"))   ScreenEffectController.PlaySkill(HoldType.Blue);
                if (GUILayout.Button("Purple スキル")) ScreenEffectController.PlaySkill(HoldType.Purple);
                if (GUILayout.Button("クリア"))        ScreenEffectController.PlayClear();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("再生中のみ動作します", MessageType.Info);
        }
    }
}
#endif
