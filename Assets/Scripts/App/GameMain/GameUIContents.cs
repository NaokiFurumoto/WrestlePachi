#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// ゲームメイン画面の UI オブジェクト参照を一元管理するクラス。
    /// Game2DContents の UI 版。ロジックは持たず参照のみ提供する。
    /// </summary>
    public sealed class GameUIContents : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private Transform _residentRoot;
        [SerializeField] private Transform _dynamicViewRoot;

        public Transform ResidentRoot    => _residentRoot;
        public Transform DynamicViewRoot => _dynamicViewRoot;
    }
}
#nullable disable
