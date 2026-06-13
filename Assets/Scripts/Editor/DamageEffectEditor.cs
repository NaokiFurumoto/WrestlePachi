#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    [CustomEditor(typeof(DamageEffect))]
    public sealed class DamageEffectEditor : UnityEditor.Editor
    {
        private int _testDamage = 125;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("テスト再生", EditorStyles.boldLabel);

            _testDamage = EditorGUILayout.IntField("ダメージ値", _testDamage);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("▶ ダメージエフェクト再生", GUILayout.Height(32)))
                {
                    var effect = (DamageEffect)target;
                    effect.PlayAsync(_testDamage, effect.destroyCancellationToken).Forget();
                }
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("再生中のみ動作します", MessageType.Info);
        }
    }
}
#endif
