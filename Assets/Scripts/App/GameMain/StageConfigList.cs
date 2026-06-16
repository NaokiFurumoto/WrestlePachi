#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    /// <summary>
    /// 全ステージのパラメーターをまとめる ScriptableObject。
    /// Assets/Resources/StageConfigList.asset として配置する。
    /// コードからは Instance プロパティ経由でアクセスする。
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfigList", menuName = "WrestlePachi/StageConfigList")]
    public sealed class StageConfigList : ScriptableObject
    {
        private static StageConfigList? _instance;

        public static StageConfigList Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<StageConfigList>("StageConfigList");
                if (_instance == null)
                    Debug.LogError("[StageConfigList] Resources/StageConfigList.asset が見つかりません。");
                return _instance!;
            }
        }

        [SerializeField] private List<StageConfig> _stages = new();

        public int Count => _stages.Count;

        public StageConfig? Get(int index)
            => index >= 0 && index < _stages.Count ? _stages[index] : null;
    }
}
#nullable disable
