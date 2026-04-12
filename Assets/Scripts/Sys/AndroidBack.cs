#nullable enable
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// Android の BACKボタン対応( Unity Editorでは ESCボタン )
    /// </summary>
    public class AndroidBack : MonoBehaviour
    {
        private ButtonEvent.EventInfo       m_EventInfo     = new ();
        
        private void Awake()
        {
            m_EventInfo.SetBackKey();
            
            if( Application.platform == RuntimePlatform.Android )
            {
                Screen.fullScreen = false;
            }
            #if !UNITY_EDITOR
            else
            {
                enabled = false;
            }
            #endif
        }
        
        private void Update()
        {
            #if UNITY_ANDROID || UNITY_EDITOR
            if( Input.GetKeyDown( KeyCode.Escape ) )
            {
                _OnPushBack();
            }
            #endif
        }
        
        /// <summary>
        /// バックボタン処理
        /// </summary>
        private void _OnPushBack()
        {
            DebugLog.Log( "push back" );
            
            // 最後に開いたViewに対してイベントを送る
            if( ButtonEvent.CheckSendEvent() == false )
            {
                return;
            }
            
            // 最後に追加したViewにイベントを送る
            // システム側があれば優先
            var view = ViewManager.GetLatestSystemView();
            if( view != null )
            {
                view.ReceiveButtonEvent( m_EventInfo );
                return;
            }
            
            view = ViewManager.GetLatestView();
            if( view != null )
            {
                view.ReceiveButtonEvent( m_EventInfo );
            }
        }
    }
}

#nullable disable
