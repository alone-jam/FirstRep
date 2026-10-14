using EjoyFramework.Core.Unity;
using UnityEngine;
namespace FirstRep.Fps
{
    // Only draws FPS-specific presentation; opening, closing, pooling and groups belong to IUIManager.
    public sealed class FpsHudForm : UIFormBehaviour
    {
        SliceBootstrap bootstrap; FpsPlayer player;
        protected override void OnFirstInit(object data) { }
        protected override void OnReuseInit(object data) { }
        public override void OnOpen(object data){base.OnOpen(data);bootstrap=(SliceBootstrap)data;player=(bootstrap.Entities.GetEntity(1).Handle as GameObject).GetComponent<FpsPlayer>();}
        public override void OnClose(bool shutdown,object data){player=null;bootstrap=null;base.OnClose(shutdown,data);}
        void OnGUI()
        {
            if(!player)return;
            GUI.Label(new Rect(20,20,650,32),player.HudText);
            GUI.Label(new Rect(20,Screen.height-40,950,30),"WASD MOVE · SHIFT SPRINT · SPACE JUMP · LMB FIRE · RMB AIM · R RELOAD · ESC PAUSE");
            GUI.Label(new Rect(Screen.width/2f-8,Screen.height/2f-12,20,24),player.HitMarker?"×":"+");
        }
    }
}
