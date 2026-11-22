#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// SkillIconEntry（Type + Icon）を1行にまとめて表示するカスタムドロワー。
    /// EnemySkillAssets._skillIcons を Inspector で設定しやすくするために用意した。
    /// </summary>
    [CustomPropertyDrawer(typeof(SkillIconEntry))]
    public sealed class SkillIconEntryDrawer : PropertyDrawer
    {
        private const float IconWidth = 40f;
        private const float Spacing   = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var contentRect = EditorGUI.PrefixLabel(position, label);

            var typeProp = property.FindPropertyRelative("Type");
            var iconProp = property.FindPropertyRelative("Icon");

            var typeWidth = contentRect.width - IconWidth - Spacing;
            var typeRect  = new Rect(contentRect.x, contentRect.y, typeWidth, contentRect.height);
            var iconRect  = new Rect(typeRect.xMax + Spacing, contentRect.y, IconWidth, contentRect.height);

            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            EditorGUI.PropertyField(iconRect, iconProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
#endif
