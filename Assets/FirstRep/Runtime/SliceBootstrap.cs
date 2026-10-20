using System;
using System.Collections;
using EjoyFramework.Core;
using EjoyFramework.Core.Coroutines;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.Fsm;
using EjoyFramework.Core.Procedure;
using EjoyFramework.Core.Resource;
using EjoyFramework.Core.Scene;
using EjoyFramework.Core.UI;
using EjoyFramework.Core.Unity;
using UnityEngine;
namespace FirstRep.Fps
{
    // Unity composition root only; progression, resources, actors and UI belong to framework modules.
    public sealed class SliceBootstrap : MonoBehaviour
    {
        public SliceContent content;
        public bool startInMenu=true;
        public bool EntryOpensMenu { get; private set; }
        public bool FailureInMenu { get; private set; }
        public BaseComponent Base { get; private set; }
        public ResourceComponent Resources { get; private set; }
        public EntityComponent Entities { get; private set; }
        public SoundComponent Sounds { get; private set; }
        public IUIManager UI { get; private set; }
        public ISceneManager Scenes { get; private set; }
        public SliceSessionHost Lifetime { get; private set; }
        public SliceExitStatus ExitStatus { get; private set; }
        public AssetBatch ContentBatch { get=>Lifetime.ContentBatch;set=>Lifetime.ContentBatch=value; }
        public bool IsPaused => Base && Base.IsGamePaused;
        public string Failure { get; private set; }
        private int nextEffectId = 10000;
        private bool applicationQuitting;
        private Coroutine retryRoutine;
        public bool CanRetry => isActiveAndEnabled && !applicationQuitting && !string.IsNullOrEmpty(Failure) && ExitStatus!=null && !ExitStatus.Requested;
        void Awake() => Enter();
        public void Enter()=>Enter(startInMenu);
        void Enter(bool menu)
        {
            if(ExitStatus!=null && !ExitStatus.Completed)throw new InvalidOperationException("Previous session has not finished its scene exit");
            CancelRetry();
            if (!content || !content.IsReady) throw new InvalidOperationException("Reviewed original content is missing; no replacement art is supplied.");
            if(!content.frameworkPrefab)throw new InvalidOperationException("Configured official Framework prefab is missing; prepare the Framework variant first.");
            EntryOpensMenu=menu;FailureInMenu=false;Failure=null;nextEffectId=10000;
            var services=Instantiate(content.frameworkPrefab);
            Base=GameEntry.RequireComponent<BaseComponent>();Resources=GameEntry.RequireComponent<ResourceComponent>();
            Entities=GameEntry.RequireComponent<EntityComponent>();Sounds=GameEntry.RequireComponent<SoundComponent>();
            UI=Framework.GetModule<IUIManager>();Scenes=Framework.GetModule<ISceneManager>();
            Lifetime=services.GetComponent<SliceSessionHost>();Lifetime.Bootstrap=this;Lifetime.Initialize();ExitStatus=Lifetime.ExitStatus;
            Entities.AddEntityGroup("Players",60,1,60,0);Entities.AddEntityGroup("Targets",60,content.targetPositions.Length,60,0);Entities.AddEntityGroup("Effects",60,32,60,0);
            Sounds.AddSoundGroup("Weapons");
            Lifetime.Base=Base;Lifetime.Entities=Entities;Lifetime.Sounds=Sounds;Lifetime.UI=UI;Lifetime.SceneManager=Scenes;
        }
        public void Pause() => ((PlayOriginalLevel)Framework.GetModule<IProcedureManager>().GetProcedure<PlayOriginalLevel>()).Pause();
        public void Resume() => ((PauseOriginalLevel)Framework.GetModule<IProcedureManager>().GetProcedure<PauseOriginalLevel>()).Resume();
        public void Fail(string reason,bool inMenu=false) { FailureInMenu=inMenu;Failure = reason; Debug.LogError("FirstRep framework integration failed: " + reason); }
        public void ShowImpact(Vector3 position, Quaternion rotation)
        {
            int id = nextEffectId++; // Explicit game entity-ID range, never a Unity object-ID cast.
            Entities.ShowEntity(id, SliceContent.ImpactEntityPath, "Effects", 0, new ActorSpawn(this, position, rotation));
        }
        public static Vector3 GroundPosition(Vector3 position)
        {
            int mask=~((1<<10)|(1<<11)|(1<<8)|(1<<12)|(1<<25)|(1<<26));
            return Physics.Raycast(position+Vector3.up*2,Vector3.down,out var floor,20,mask,QueryTriggerInteraction.Ignore) ? floor.point+Vector3.up*.05f : position;
        }
        public void Exit()
        {
            CancelRetry();
            if(ExitStatus==null || ExitStatus.Requested || ExitStatus.Completed)return;
            var current=GameEntry.Procedure && GameEntry.Procedure.IsInitialized ? GameEntry.Procedure.CurrentProcedure as OriginalLevelProcedure : null;
            if(current!=null)current.Exit();else Lifetime.BeginExit();
        }
        public void Retry()
        {
            if(!CanRetry)return;
            Restart(FailureInMenu);
        }
        public void StartLevel()=>((MainMenuOriginalLevel)GameEntry.Procedure.GetProcedure<MainMenuOriginalLevel>()).StartLevel();
        public bool CanReturnToMenu => isActiveAndEnabled && ExitStatus!=null && !ExitStatus.Requested && !FailureInMenu &&
            GameEntry.Procedure && GameEntry.Procedure.IsInitialized && (GameEntry.Procedure.CurrentProcedure is PlayOriginalLevel || GameEntry.Procedure.CurrentProcedure is PauseOriginalLevel || GameEntry.Procedure.CurrentProcedure is FailedOriginalLevel);
        public void ReturnToMenu(){if(CanReturnToMenu)Restart(true);}
        void Restart(bool menu)
        {
            var previous=ExitStatus;Exit();
            retryRoutine=StartCoroutine(EnterAfterExit(previous,menu));
        }
        IEnumerator EnterAfterExit(SliceExitStatus previous,bool menu)
        {
            // This owner survives Framework shutdown; no new module is requested
            // until SceneLease release and native service destruction both finish.
            while(!previous.Completed)yield return null;
            retryRoutine=null;
            if(isActiveAndEnabled && !applicationQuitting)Enter(menu);
        }
        void CancelRetry()
        {
            if(retryRoutine==null)return;
            StopCoroutine(retryRoutine);retryRoutine=null;
        }
        // Unity closes native scenes itself on application quit. BaseComponent may
        // already have shut the framework down before this component is destroyed.
        void OnApplicationQuit(){applicationQuitting=true;}
        void OnDestroy(){if(!applicationQuitting)Exit();}

    }
    public sealed class ActorSpawn
    {
        public readonly SliceBootstrap Bootstrap; public readonly Vector3 Position; public readonly Quaternion Rotation;
        public ActorSpawn(SliceBootstrap bootstrap, Vector3 position, Quaternion rotation) { Bootstrap=bootstrap;Position=position;Rotation=rotation; }
        public static ActorSpawn From(object data) => (ActorSpawn)((EntityData)data).UserData;
    }
}
