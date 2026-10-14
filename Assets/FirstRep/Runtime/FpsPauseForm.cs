using EjoyFramework.Core.Unity;
using UnityEngine;
namespace FirstRep.Fps
{
    public sealed class FpsPauseForm : UIFormBehaviour
    {
        SliceBootstrap bootstrap;
        protected override void OnFirstInit(object data) { }
        protected override void OnReuseInit(object data) { }
        public override void OnOpen(object data){base.OnOpen(data);bootstrap=(SliceBootstrap)data;}
        public override void OnClose(bool shutdown,object data){bootstrap=null;base.OnClose(shutdown,data);}
        void OnGUI(){if(bootstrap && GUI.Button(new Rect(Screen.width/2-90,Screen.height/2-20,180,40),"Resume"))bootstrap.Resume();}
    }
}
