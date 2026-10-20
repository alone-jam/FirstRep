using System;
using System.Collections.Generic;
using EjoyFramework.Core;
using EjoyFramework.Core.Unity;
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
    public abstract class OriginalLevelProcedure : ProcedureBase
    {
        protected IFsm<IProcedureManager> Owner { get; private set; }
        protected override void OnEnter(IFsm<IProcedureManager> fsm){Owner=fsm;}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown){Owner=null;}
        public void Exit(){if(Owner!=null)ChangeState<ExitOriginalLevel>(Owner);}
    }
    public sealed class LoadOriginalLevel : OriginalLevelProcedure
    {
        readonly SliceBootstrap boot;bool entered;
        bool initialized, requested, actorsRequested; string failure;
        IEntityManager entities;
        public LoadOriginalLevel(SliceBootstrap boot) => this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            base.OnEnter(fsm);entered=true;initialized=requested=actorsRequested=false;failure=null;
            entities=Framework.GetModule<IEntityManager>();entities.ShowEntityFailure+=OnActorFailure;
            boot.Resources.InitializeAsync(()=>{if(entered)initialized=true;},reason=>{if(entered)failure=reason;});
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
                boot.Lifetime.AcquireScenes(boot.content.levelScenes);
            }
            foreach(var lease in boot.Lifetime.Scenes)
            {
                if(lease.State==SceneLeaseState.Failed){failure=lease.Error;return;}
                if(lease.State!=SceneLeaseState.Ready)return;
            }
            if(!boot.ContentBatch.IsDone)return;
            if(boot.ContentBatch.FailedCount!=0){failure="Framework content preload failed; no direct-reference fallback is allowed.";return;}
            if(!actorsRequested)
            {
                actorsRequested=true;
                try
                {
                    boot.Entities.ShowEntity(1,SliceContent.PlayerEntityPath,"Players",0,new ActorSpawn(boot,SliceBootstrap.GroundPosition(boot.content.spawnPosition),Quaternion.Euler(0,boot.content.spawnYaw,0)));
                    for(int i=0;i<boot.content.targetPositions.Length && failure==null;i++)
                    {
                        var position=SliceBootstrap.GroundPosition(boot.content.targetPositions[i]);var facing=boot.content.spawnPosition-position;facing.y=0;
                        boot.Entities.ShowEntity(100+i,boot.content.TargetAssetPath,"Targets",0,new ActorSpawn(boot,position,Quaternion.LookRotation(facing)));
                    }
                }
                catch(FrameworkException error){failure="Actor request rejected: "+error.Message;}
                if(failure!=null)return;
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
        void OnActorFailure(object sender,ShowEntityFailureEventArgs error)
        {
            if(!entered || failure!=null || (error.EntityGroupName!="Players" && error.EntityGroupName!="Targets"))return;
            if(!(error.UserData is EntityData data) || !(data.UserData is ActorSpawn request) || !ReferenceEquals(request.Bootstrap,boot))return;
            // Framework returns the event args to ReferencePool after this callback.
            failure=$"Entity '{error.EntityAssetName}' ({error.EntityId}) failed: {error.ErrorMessage}";
        }
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown)
        {
            entered=false;
            if(entities!=null)entities.ShowEntityFailure-=OnActorFailure;
            entities=null;base.OnLeave(fsm,shutdown);
        }
    }
    public sealed class PlayOriginalLevel : OriginalLevelProcedure
    {
        readonly SliceBootstrap boot;IFsm<IProcedureManager> owner;int hud;
        public PlayOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            base.OnEnter(fsm);owner=fsm;boot.Base.ResumeGame();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            hud=boot.UI.OpenUIForm(SliceContent.HudPath,"FirstRep",0,false,boot);
        }
        public void Pause(){if(owner!=null)ChangeState<PauseOriginalLevel>(owner);}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown){boot.UI.CloseUIForm(hud);hud=0;owner=null;base.OnLeave(fsm,shutdown);}
    }
    public sealed class PauseOriginalLevel : OriginalLevelProcedure
    {
        readonly SliceBootstrap boot;IFsm<IProcedureManager> owner;int menu;
        public PauseOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            base.OnEnter(fsm);owner=fsm;boot.Base.PauseGame();Framework.GetModule<IInputManager>().PushContext(InputContext.Menu);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            menu=boot.UI.OpenUIForm(SliceContent.PausePath,"FirstRep",10,true,boot);
        }
        public void Resume(){if(owner!=null)ChangeState<PlayOriginalLevel>(owner);}
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown)
        {
            boot.UI.CloseUIForm(menu);menu=0;Framework.GetModule<IInputManager>().PopContext();boot.Base.ResumeGame();owner=null;base.OnLeave(fsm,shutdown);
        }
    }
    public sealed class ExitOriginalLevel : OriginalLevelProcedure
    {
        readonly SliceBootstrap boot;
        public ExitOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm){base.OnEnter(fsm);boot.Lifetime.BeginExit();}
    }
    public sealed class FailedOriginalLevel : OriginalLevelProcedure
    {
        readonly SliceBootstrap boot;int menu;bool menuContext;IInputManager input;
        public FailedOriginalLevel(SliceBootstrap boot)=>this.boot=boot;
        protected override void OnEnter(IFsm<IProcedureManager> fsm)
        {
            base.OnEnter(fsm);menu=0;menuContext=false;boot.Base.PauseGame();
            boot.Entities.HideAllLoadingEntities();boot.Entities.HideAllLoadedEntities();boot.Sounds.StopAllLoadedSounds();
            if(boot.ContentBatch!=null && !boot.ContentBatch.IsDone)boot.ContentBatch.Cancel();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            // Only use the resource/UI module path. If initialization or the menu
            // itself failed, retain Failure/Exit without inventing a fallback UI.
            if(!Framework.GetModule<IResourceManager>().TryGetCachedAsset<UnityEngine.GameObject>(SliceContent.PausePath,out _))return;
            input=Framework.GetModule<IInputManager>();input.PushContext(InputContext.Menu);menuContext=true;
            menu=boot.UI.OpenUIForm(SliceContent.PausePath,"FirstRep",10,true,boot);
        }
        protected override void OnLeave(IFsm<IProcedureManager> fsm,bool shutdown)
        {
            if(menu!=0)boot.UI.CloseUIForm(menu);menu=0;
            if(menuContext)input.PopContext();menuContext=false;input=null;
            base.OnLeave(fsm,shutdown);
        }
    }
}
