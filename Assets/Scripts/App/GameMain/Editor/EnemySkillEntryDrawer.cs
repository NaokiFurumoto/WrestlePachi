#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// EnemySkillEntry（Type + Level）を1行にまとめて表示するカスタムドロワー。
    /// EnemyData の SignatureSkills / RematchExtraSkills 配列を Inspector で設定しやすくするために用意した。
    /// </summary>
    [CustomPropertyDrawer(typeof(EnemySkillEntry))]
    public sealed class EnemySkillEntryDrawer : PropertyDrawer
    {
        private const float LevelLabelWidth = 20f;
        private const float LevelFieldWidth = 32f;
        private const float Spacing         = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var contentRect = EditorGUI.PrefixLabel(position, label);

            var typeProp  = property.FindPropertyRelative("Type");
            var levelProp = property.FindPropertyRelative("Level");

            var typeWidth = contentRect.width - LevelLabelWidth - LevelFieldWidth - Spacing;
            var typeRect       = new Rect(contentRect.x, contentRect.y, typeWidth, contentRect.height);
            var levelLabelRect = new Rect(typeRect.xMax + Spacing, contentRect.y, LevelLabelWidth, contentRect.height);
            var levelFieldRect = new Rect(levelLabelRect.xMax, contentRect.y, LevelFieldWidth, contentRect.height);

            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            EditorGUI.LabelField(levelLabelRect, "Lv");
            EditorGUI.PropertyField(levelFieldRect, levelProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
#endif
