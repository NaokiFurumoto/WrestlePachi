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
        [SerializeField, Min( 0f )] private float _cooldownSeconds = 0.15f;

        private float _lastLaunchTime = float.NegativeInfinity;
        private GameObject _cachedBallPrefab;

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

            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
            rigidbody2D.AddForce( launchDirection * _launchImpulse, ForceMode2D.Impulse );

            _lastLaunchTime = Time.time;
        }

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
        /// </summary>
        private Vector2 _GetLaunchDirection()
        {
            if( _launchDirection.sqrMagnitude >= MinDirectionSqrMagnitude )
            {
                return _launchDirection.normalized;
            }

            Debug.LogWarning( "BallLauncher: 発射方向が 0 ベクトルだったため、右斜め上の既定値を使用します。", this );
            return new Vector2( 1f, 0.8f ).normalized;
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
