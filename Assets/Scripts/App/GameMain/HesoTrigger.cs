#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// へそ（中心穴）に配置するトリガー。
    /// Rigidbody2D を持つオブジェクト（玉）が触れたら玉を消す。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HesoTrigger : MonoBehaviour
    {
        public event System.Action? OnEntered;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Rigidbody2D を持つ動的オブジェクト（玉）のみ反応
            if (other.attachedRigidbody == null) return;

            OnEntered?.Invoke();
            Destroy(other.gameObject);
        }
    }
}
#nullable disable
