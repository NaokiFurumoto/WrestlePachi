#nullable enable
using UnityEngine;

namespace App
{
    /// <summary>
    /// アウト口（死に穴）に配置するトリガー。
    /// 玉が入ったら消去するだけで、ゲームへの通知はしない。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DeathHoleTrigger : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            Destroy(other.gameObject);
        }
    }
}
#nullable disable
