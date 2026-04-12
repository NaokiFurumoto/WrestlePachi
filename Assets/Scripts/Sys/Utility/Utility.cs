#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// 汎用処理
    /// </summary>
    public static class Utility
    {
        #region List関連
        
        /// <summary>
        /// シャッフル
        /// </summary>
        public static IOrderedEnumerable<T> Shuffle<T>( IEnumerable<T> source )
        {
            // NOTE:GC問題になりそうなら変更する
            return source.OrderBy( i => Guid.NewGuid() );
        }

        public static List<T> ShuffleToList<T>( IEnumerable<T> source )
        {
            return Shuffle( source ).ToList();
        }

        public static T[] ShuffleToArray<T>( IEnumerable<T> source )
        {
            return Shuffle( source ).ToArray();
        }

        public static string ToStringList<T>( this List<T> list )
        {
            using( var sb = ZString.CreateStringBuilder() )
            {
                for( int i = 0; i < list.Count; ++i )
                {
                    sb.Append( list[i] );
                    sb.Append( "," );
                }
                return sb.ToString();
            }
        }
        
        #endregion List関連

        #region GameObject関連

        /// <summary>
        /// null許容 SetActive()
        /// </summary>
        public static void SetActiveNullable( this GameObject? gobj, bool isActive )
        {
            if( gobj != null )
            {
                gobj.SetActive( isActive );
            }
        }

        public static void SetActiveNullable( this Component? component, bool isActive )
        {
            if( component != null )
            {
                component.gameObject.SetActive( isActive );
            }
        }

        #endregion GameObject関連

        #region Behaviour関連
        
        // RenderなどBehaviourを継承していないものもあるので万能ではない
        public static void SetEnableNullable( this Behaviour? behaviour, bool isEnable )
        {
            if( behaviour != null )
            {
                behaviour.enabled = isEnable;
            }
        } 
        
        #endregion Behaviour関連

        #region Transform関連

        public static void SetLocalPos( this Transform? trans, Vector3 pos )
        {
            if( trans != null )
            {
                trans.localPosition = pos;
            }
        }
        
        public static void SetLocalPos( this Transform? trans, ref Vector3 pos )
        {
            if( trans != null )
            {
                trans.localPosition = pos;
            }
        }
        
        #endregion Transform関連

        /// <summary>
        /// 現在位置からLocalPositionの補間
        /// </summary>
        public static async UniTask LerpLocalPosition( Transform targetTrans, Vector3 dstPos, float duration )
        {
            float elapse = 0f;
            Vector3 srcPos = targetTrans.localPosition;
            
            while( true )
            {
                float rate = EasingUtil.EaseInOutQuart( Mathf.Clamp01( elapse / duration ) );
                targetTrans.localPosition = Vector3.Lerp( srcPos, dstPos, rate );

                if( elapse >= duration )
                {
                    break;
                }
                
                elapse += Time.deltaTime;
                await  UniTask.Yield();
            }
        }
        
        public static async UniTask LerpLocalRotation( Transform targetTrans, Quaternion dstRot, float duration )
        {
            float elapse = 0f;
            Quaternion srcRot = targetTrans.localRotation;
            
            while( true )
            {
                float rate = EasingUtil.EaseInOutQuart( Mathf.Clamp01( elapse / duration ) );
                targetTrans.localRotation = Quaternion.Slerp( srcRot, dstRot, rate );

                if( elapse >= duration )
                {
                    break;
                }
                
                elapse += Time.deltaTime;
                await  UniTask.Yield();
            }
        }
        
        /// <summary>
        /// WorldPositionの補間
        /// </summary>
        public static async UniTask LerpWorldPosition( Transform targetTrans, Vector3 dstPos, float duration )
        {
            float elapse = 0f;
            Vector3 srcPos = targetTrans.position;

            while( true )
            {
                float rate = EasingUtil.EaseInOutQuart( Mathf.Clamp01( elapse / duration ) );
                targetTrans.position = Vector3.Lerp( srcPos, dstPos, rate );

                if( elapse >= duration )
                {
                    break;
                }
                
                elapse += Time.deltaTime;
                await  UniTask.Yield();
            }
        }
        
        // =========================================================================
        // エディタ
        // =========================================================================
        #if UNITY_EDITOR
        
        public static void EditorGUI_DrawOpenFileButton( [CallerFilePath] string filePath = "",
                                                         [CallerLineNumber] int lineNumber = 0 )
        {
            if( GUILayout.Button( "ソースコードを開く" ) )
            {
                InternalEditorUtility.OpenFileAtLineExternal( filePath, lineNumber );
            }
        }
        
        #endif //UNITY_EDITOR
    }
}

#nullable disable
