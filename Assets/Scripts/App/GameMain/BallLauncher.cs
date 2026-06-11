using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{
    /// <summary>
    /// Space キー入力で球を生成し、右斜め上へ打ち出すランチャーです。
    /// </summary>
    public sealed class BallLauncher : MonoBehaviour
    {
        private const string DefaultBallPrefabPath = "Prefabs/GameMain/Pachinco/Ball";
        private const float MinDirectionSqrMagnitude = 0.0001f;

        [Header( "生成設定" )]
        [SerializeField] private GameObject _ballPrefab;
        [SerializeField] private Vector3 _spawnOffset = Vector3.zero;

        [Header( "発射設定" )]
        [SerializeField] private Vector2 _launchDirection = new Vector2( 1f, 0.8f );
        [SerializeField, Min( 0f )] private float _launchImpulse = 12f;
        [SerializeField, Min( 0f )] private float _cooldownSeconds = 0.4f;

        [Header( "発射ばらつき（シミュレーター用）" )]
        [SerializeField, Range( 0f, 30f )] private float _angleSpread    = 3f;   // 角度ランダム幅（±度）
        [SerializeField, Range( 0f, 0.3f )] private float _impulseVariance = 0.05f; // 強さランダム幅（±割合）

        private float _lastLaunchTime = float.NegativeInfinity;
        private GameObject _cachedBallPrefab;

        // 発射した玉をすべて追跡する（シミュレーター終了時の一括消去に使用）
        private readonly System.Collections.Generic.List<GameObject> _spawnedBalls = new();

        /// <summary>
        /// 連鎖数に応じて複数の球を順番に発射する。
        /// GameMainController.OnChainCompleted から呼ぶ。
        /// </summary>
        public async UniTaskVoid LaunchAsync(int count, CancellationToken ct)
        {
            for (var i = 0; i < count && !ct.IsCancellationRequested; i++)
            {
                _LaunchBall();
                await UniTask.Delay(Mathf.RoundToInt(_cooldownSeconds * 1000) + 100, cancellationToken: ct);
            }
        }

        private void Update()
        {
            var isLaunchTriggered = _IsLaunchTriggered();
            if( isLaunchTriggered == false || _CanLaunch() == false )
            {
                return;
            }

            _LaunchBall();
        }

        /// <summary>
        /// 発射入力が押されたフレームかを返します。
        /// </summary>
        private static bool _IsLaunchTriggered()
        {
            return Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
        }

        /// <summary>
        /// 発射クールダウンが終わっているかを返します。
        /// </summary>
        private bool _CanLaunch()
        {
            return Time.time >= _lastLaunchTime + _cooldownSeconds;
        }

        /// <summary>
        /// 球を生成して、設定した方向へ打ち出します。
        /// </summary>
        private void _LaunchBall()
        {
            var prefab = _ResolveBallPrefab();
            if( prefab == null )
            {
                Debug.LogWarning( "BallLauncher: Ball の prefab が設定されておらず、Resources からも見つかりませんでした。", this );
                return;
            }

            var spawnPosition = transform.TransformPoint( _spawnOffset );
            var instantiatedObject = Instantiate( ( Object )prefab, spawnPosition, Quaternion.identity );
            var ballObject = _ToGameObject( instantiatedObject );
            if( ballObject == null )
            {
                Debug.LogWarning( $"BallLauncher: 生成結果を GameObject として扱えませんでした。型: {instantiatedObject.GetType().Name}", this );
                Destroy( instantiatedObject );
                return;
            }

            if( ballObject.TryGetComponent<Rigidbody2D>( out var rigidbody2D ) == false )
            {
                Debug.LogWarning( "BallLauncher: 生成した Ball prefab に Rigidbody2D がありません。", ballObject );
                Destroy( ballObject );
                return;
            }

            var launchDirection = _GetLaunchDirection();
            var launchImpulse   = _GetLaunchImpulse();

            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
            rigidbody2D.AddForce( launchDirection * launchImpulse, ForceMode2D.Impulse );

            _lastLaunchTime = Time.time;
            _spawnedBalls.Add(ballObject);
        }

        /// <summary>シミュレーター用：1球をクールダウン無視で即時発射する。</summary>
        public void LaunchOneForSimulator() => _LaunchBall();

        /// <summary>シーン上のすべての発射済み玉を即時破棄する。</summary>
        public void ClearAllBalls()
        {
            foreach (var ball in _spawnedBalls)
                if (ball != null) Destroy(ball);
            _spawnedBalls.Clear();
        }

#if UNITY_EDITOR
        /// <summary>デバッグ用（後方互換）</summary>
        public void Debug_ClearAllBalls() => ClearAllBalls();
#endif

        /// <summary>
        /// UnityEngine.Object から GameObject を安全に取り出します。
        /// </summary>
        private static GameObject _ToGameObject( Object obj )
        {
            if( obj is GameObject gameObject )
            {
                return gameObject;
            }

            if( obj is Component component )
            {
                return component.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 発射方向を正規化して返します。未設定時は右斜め上を使います。
        /// _angleSpread の範囲でランダムな角度ブレを加えます。
        /// </summary>
        private Vector2 _GetLaunchDirection()
        {
            var baseDir = _launchDirection.sqrMagnitude >= MinDirectionSqrMagnitude
                ? _launchDirection.normalized
                : new Vector2( 1f, 0.8f ).normalized;

            if( _launchDirection.sqrMagnitude < MinDirectionSqrMagnitude )
                Debug.LogWarning( "BallLauncher: 発射方向が 0 ベクトルだったため、右斜め上の既定値を使用します。", this );

            // ±_angleSpread 度のランダム回転を加える
            var angleDeg  = Random.Range( -_angleSpread, _angleSpread );
            var rad       = angleDeg * Mathf.Deg2Rad;
            var cos       = Mathf.Cos( rad );
            var sin       = Mathf.Sin( rad );
            return new Vector2(
                baseDir.x * cos - baseDir.y * sin,
                baseDir.x * sin + baseDir.y * cos
            );
        }

        /// <summary>
        /// 発射強度に ±_impulseVariance のランダムばらつきを乗せて返します。
        /// </summary>
        private float _GetLaunchImpulse()
        {
            return _launchImpulse * Random.Range( 1f - _impulseVariance, 1f + _impulseVariance );
        }

        /// <summary>
        /// Inspector で未設定のときは Resources から Ball prefab を読み込みます。
        /// </summary>
        private GameObject _ResolveBallPrefab()
        {
            if( _ballPrefab != null )
            {
                return _ballPrefab;
            }

            if( _cachedBallPrefab == null )
            {
                _cachedBallPrefab = Resources.Load<GameObject>( DefaultBallPrefabPath );
            }

            return _cachedBallPrefab;
        }
    }
}
