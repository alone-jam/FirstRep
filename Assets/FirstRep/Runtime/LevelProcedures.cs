using System;
using System.Collections.Generic;
using EjoyFramework.Core;
using EjoyFramework.Core.Fsm;
using EjoyFramework.Core.Input;
using EjoyFramework.Core.Procedure;
using EjoyFramework.Core.Resource;
using EjoyFramework.Core.Scene;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.UI;
using UnityEngine;
namespace FirstRep.Fps
{
    public sealed class LoadOriginalLevel : ProcedureBase
    {
        readonly SliceBootstrap boot; IFsm<IProcedureManager> owner;
        readonly HashSet<string> loaded = new HashSet<string>();
        bool initialized, requested, actorsRequested; string failure;
        public LoadOriginalLevel(SliceBootstrap boot) => this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            owner=fsm; loaded.Clear();initialized=requested=actorsRequested=false;failure=null;
            boot.Scenes.LoadSceneSuccess+=Loaded;boot.Scenes.LoadSceneFailure+=Failed;
            boot.Resources.InitializeAsync(()=>initialized=true,reason=>failure=reason);
        }
        protected override void OnUpdate(IFsm<IProcedureManager> fsm,float elapsed,float real)
        {
            if(failure!=null){boot.Fail(failure);ChangeState<FailedOriginalLevel>(fsm);return;}
            if(!initialized)return;
            if(!requested)
            {
                requested=true;
                boot.ContentBatch=Framework.GetModule<IResourceManager>().CreateAssetBatch();
                foreach(var path in new[]{SliceContent.PlayerEntityPath,boot.content.TargetAssetPath,SliceContent.ImpactEntityPath,SliceContent.HudPath,SliceContent.PausePath,boot.content.ShotAudioPath})boot.ContentBatch.Add(path);
                boot.ContentBatch.Start();
                foreach(var path in boot.content.levelScenes)boot.Scenes.LoadScene(path,0,this);
            }
            if(!boot.ContentBatch.IsDone || loaded.Count!=boot.content.levelScenes.Length)return;
            if(boot.ContentBatch.FailedCount!=0){failure="Framework content preload failed; no direct-reference fallback is allowed.";return;}
            if(!actorsRequested)
            {
                actorsRequested=true;
                boot.Entities.ShowEntity(1,SliceContent.PlayerEntityPath,"Players",0,new ActorSpawn(boot,SliceBootstrap.GroundPosition(boot.content.spawnPosition),Quaternion.Euler(0,boot.content.spawnYaw,0)));
                for(int i=0;i<boot.content.targetPositions.Length;i++)
                {
                    var position=SliceBootstrap.GroundPosition(boot.content.targetPositions[i]);var facing=boot.content.spawnPosition-position;facing.y=0;
                    boot.Entities.ShowEntity(100+i,boot.content.TargetAssetPath,"Targets",0,new ActorSpawn(boot,position,Quaternion.LookRotation(facing)));
                }
            }
            if(boot.Entities.EntityCount==1+boot.content.targetPositions.Length)
            {
                var entities=boot.Entities.GetAllLoadedEntities();
                for(int i=0;i<entities.Length;i++)for(int j=i+1;j<entities.Length;j++)
                    if(ReferenceEquals(entities[i].Handle,entities[j].Handle))
                    {
                        boot.Fail("Upstream entity instance aliasing blocks concurrent actors; see EjoyFramework issue #17.");
                        ChangeState<FailedOriginalLevel>(fsm);return;
                    }
                ChangeState<PlayOriginalLevel>(fsm);
            }
        }
        void Loaded(object sender,LoadSceneSuccessEventArgs e){if(ReferenceEquals(e.UserData,this))loaded.Add(e.SceneAssetName);}
        void Failed(object sender,LoadSceneFailureEventArgs e){if(ReferenceEquals(e.UserData,this))failure=e.ErrorMessage;}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown){boot.Scenes.LoadSceneSuccess-=Loaded;boot.Scenes.LoadSceneFailure-=Failed;owner=null;}
    }
    public sealed class PlayOriginalLevel : ProcedureBase
    {
        readonly SliceBootstrap boot;IFsm<IProcedureManager> owner;int hud;
        public PlayOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            owner=fsm;boot.Base.ResumeGame();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            hud=boot.UI.OpenUIForm(SliceContent.HudPath,"FirstRep",0,false,boot);
        }
        public void Pause(){if(owner!=null)ChangeState<PauseOriginalLevel>(owner);}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown){boot.UI.CloseUIForm(hud);hud=0;owner=null;}
    }
    public sealed class PauseOriginalLevel : ProcedureBase
    {
        readonly SliceBootstrap boot;IFsm<IProcedureManager> owner;int menu;
        public PauseOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            owner=fsm;boot.Base.PauseGame();Framework.GetModule<IInputManager>().PushContext(InputContext.Menu);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            menu=boot.UI.OpenUIForm(SliceContent.PausePath,"FirstRep",10,true,boot);
        }
        public void Resume(){if(owner!=null)ChangeState<PlayOriginalLevel>(owner);}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown)
        {
            boot.UI.CloseUIForm(menu);menu=0;Framework.GetModule<IInputManager>().PopContext();boot.Base.ResumeGame();owner=null;
        }
    }
    public sealed class FailedOriginalLevel : ProcedureBase
    {
        readonly SliceBootstrap boot; public FailedOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm){boot.Base.PauseGame();}
    }
}
