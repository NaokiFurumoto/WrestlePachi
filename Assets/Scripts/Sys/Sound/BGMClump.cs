#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using GameSys;
using UnityEditor;
#endif

[CreateAssetMenu( menuName = "ScriptableObjects/BGMClump", order = 1)]
public class BGMClump : ScriptableObject
{
    [Serializable]
    public class ClipInfo
    {
        public  AudioClip?          clip    = null;
        // public  float               volume  = 1f;
        
        #if UNITY_EDITOR
        
        const   string      CAP_INTRO       = "Intro";
        const   string      CAP_LOOP        = "Loop";
        
        static  readonly    private Color       COLOR_1                 = new Color( 0f, 1f, 1f );
        static  readonly    private Color       COLOR_2                 = new Color( 0f, 0f, 1f );
        
        public bool DrawGUI( bool isLoop )
        {
            bool isDelete = false;

            GUI.backgroundColor = ( isLoop == false ) ? COLOR_1 : COLOR_2;
            using( var h = new EditorGUILayout.HorizontalScope( "box" ) )
            {
                GUI.backgroundColor = Color.white;

                using( var v = new EditorGUILayout.VerticalScope() )
                {
                    var width = GUILayout.Width( 90 );
                    var capW = GUILayout.Width( 70 );

                    using ( var e = new EditorGUILayout.HorizontalScope() )
                    {
                        EditorGUILayout.LabelField( isLoop ? CAP_LOOP : CAP_INTRO, capW );
                    }
                    
                    using ( var e = new EditorGUILayout.HorizontalScope() )
                    {
                        EditorGUILayout.LabelField( "clip", capW );
                        clip = EditorGUILayout.ObjectField( clip, typeof(AudioClip), false ) as AudioClip;
                    }
                    
                    // using ( var e = new EditorGUILayout.HorizontalScope() )
                    // {
                    //     EditorGUILayout.LabelField( "volume", capW );
                    //     volume = EditorGUILayout.FloatField( volume );
                    // }
                }
            }
            
            return isDelete;
        }
        
        #endif // UNITY_EDITOR
    }
    
    [SerializeField]
    public  float               volume  = 1.0f;
    
    [SerializeField]
    private ClipInfo            clipInfo    = new ClipInfo();
    
    public ClipInfo     clip        =>      clipInfo;
    
    #region エディタ

    //
    #if UNITY_EDITOR

    [CustomEditor(typeof( BGMClump ) )]
    public class Editor_BGMClump : Editor
    {
        private BGMClump?           m_Target                = null;
        
        // ------------------------------------------------------------------
        // 有効化
        // ------------------------------------------------------------------
        private void OnEnable()
        {
            m_Target = target as BGMClump;
        }

        // ------------------------------------------------------------------
        // 描画
        // ------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            var capW = GUILayout.Width( 70 );
            
            if( m_Target == null )
                return;
            
            serializedObject.Update();
            
            EditorGUILayout.Space( 5 );
            
            EditorGUILayout.Space( 10 );
            
            using( var change = new EditorGUI.ChangeCheckScope() )
            {
                using ( var e = new EditorGUILayout.HorizontalScope() )
                {
                    EditorGUILayout.LabelField( "volume", capW );
                    m_Target.volume = EditorGUILayout.FloatField( m_Target.volume );
                }
                
                m_Target.clipInfo.DrawGUI( true );
                
                if( change.changed )
                {
                    EditorUtility.SetDirty( m_Target );
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
    
    #endif //UNITY_EDITOR
    #endregion エディタ
}
#nullable disable
