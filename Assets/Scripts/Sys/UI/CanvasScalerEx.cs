#nullable enable
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameSys
{
    public class CanvasScalerEx : CanvasScaler
    {
        public enum ELayer
        {
            Back,
            Center,
            Front,
        }
        
        [SerializeField]
        private ELayer              m_Layer = ELayer.Center;
        
        [SerializeField]
        private Transform?          SafeAreaTrans                   = null;

        public ELayer               Layer   => m_Layer;
        
        /// <summary>
        /// 生成時
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            
            #if UNITY_EDITOR
            if( EditorApplication.isPlaying == false )
            {
                return;
            }
            #endif
            
            SetParameter();
        }
        
        /// <summary>
        /// パラメータ設定
        /// </summary>
        public void SetParameter()
        {
            uiScaleMode = ScaleMode.ConstantPixelSize;
            if( Layer == ELayer.Center )
            {
                scaleFactor = GameScreen.UiCenterScale;

                if( SafeAreaTrans != null )
                {
                    var pos = SafeAreaTrans.localPosition;
                    pos.y = GameScreen.UiCenterOffsetY;
                    SafeAreaTrans.localPosition = pos;
                    
                    var rectTrans = SafeAreaTrans as RectTransform;
                    if( rectTrans != null )
                    {
                        rectTrans.sizeDelta = new Vector2( GameScreen.UiCenterWidth, GameScreen.UiCenterHeight );
                    }
                }
            }
            else
            {
                scaleFactor = GameScreen.UiBackgroundScale;
            }
        }
        
        #if UNITY_EDITOR
        [CustomEditor(typeof(CanvasScalerEx), true), CanEditMultipleObjects]
        public class Editor_CanvasScalerEx : UnityEditor.UI.CanvasScalerEditor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                
                base.OnInspectorGUI();
                
                var tmp = target as CanvasScalerEx;
                if( tmp == null )
                {
                    return;
                }
                
                tmp.m_Layer = (CanvasScalerEx.ELayer)EditorGUILayout.EnumPopup( "レイヤータイプ", tmp.m_Layer );
                tmp.SafeAreaTrans = (Transform)EditorGUILayout.ObjectField( "SafeArea Transform", tmp.SafeAreaTrans, typeof(Transform), true );
            }
        }
        #endif
    }
}
#nullable disable

