#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// ポケット（賞球口）に配置するトリガー。
    /// 玉が入ったら消去し、入賞を通知する。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PocketTrigger : MonoBehaviour
    {
        public event System.Action? OnEntered;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;

            OnEntered?.Invoke();
            Destroy(other.gameObject);
        }
    }
}
#nullable disable
