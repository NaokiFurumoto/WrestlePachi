using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace App
{
    /// <summary>
    /// 2Dゲームの表示まとめ役
    /// ・View層のみ
    /// ・ロジックは持たない
    /// </summary>
    public class Game2DContents : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private Camera _mainCamera;

        [Header("Systems")]

        [Header("Environment")]
        [SerializeField] private Light2D _mainLight; // ライト

        [Header("Effects")]
        [SerializeField] private Transform _effectRoot;

        public Camera MainCamera => _mainCamera;
        public Light2D MainLight => _mainLight;
        public Transform EffectRoot => _effectRoot;

        /// <summary>
        /// 初期化（必要なら）
        /// </summary>
        public void Initialize()
        {
            // 今は何もしなくてOK
        }
    }
}

