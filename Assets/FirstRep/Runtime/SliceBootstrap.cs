using System;
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
        void Awake() => Enter();
        public void Enter()
        {
            if(ExitStatus!=null && !ExitStatus.Completed)throw new InvalidOperationException("Previous session has not finished its scene exit");
            if (!content || !content.IsReady) throw new InvalidOperationException("Reviewed original content is missing; no replacement art is supplied.");
            var services=new GameObject("FirstRep framework session");
            Base = services.AddComponent<BaseComponent>();
            Lifetime=services.AddComponent<SliceSessionHost>();Lifetime.Initialize();ExitStatus=Lifetime.ExitStatus;
            Failure=null;nextEffectId=10000;
            services.AddComponent<InputComponent>(); // Default upstream Input System backend, now Mouse0/1 capable.
            Resources = services.AddComponent<ResourceComponent>();
            Entities = services.AddComponent<EntityComponent>();
            Entities.AddEntityGroup("Players", 60, 1, 60, 0);
            Entities.AddEntityGroup("Targets", 60, content.targetPositions.Length, 60, 0);
            Entities.AddEntityGroup("Effects", 60, 32, 60, 0);
            Sounds = services.AddComponent<SoundComponent>(); Sounds.AddSoundGroup("Weapons");
            UI = Framework.GetModule<IUIManager>();
            UI.SetUIFormHelper(services.AddComponent<DefaultUIFormHelper>());
            var group = new GameObject("FirstRep UI group").AddComponent<UIGroupCanvasHelper>();
            group.transform.SetParent(services.transform, false);
            group.Configure("FirstRep", 0, new Vector2(1280,720), .5f);
            UI.AddUIGroup("FirstRep", 0, group);
            Scenes = Framework.GetModule<ISceneManager>();
            Scenes.SetHelper(new DefaultSceneHelper(Framework.GetModule<ICoroutineManager>(), null));
            Lifetime.Base=Base;Lifetime.Entities=Entities;Lifetime.Sounds=Sounds;Lifetime.UI=UI;Lifetime.SceneManager=Scenes;
            if(!UnityEngine.EventSystems.EventSystem.current)
            {
                var events=new GameObject("FirstRep UI events",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                events.transform.SetParent(services.transform,false);
            }
            var procedures = Framework.GetModule<IProcedureManager>();
            procedures.Initialize(Framework.GetModule<IFsmManager>(), new LoadOriginalLevel(this), new PlayOriginalLevel(this), new PauseOriginalLevel(this), new FailedOriginalLevel(this),new ExitOriginalLevel(this));
            procedures.StartProcedure<LoadOriginalLevel>();
        }
        public void Pause() => ((PlayOriginalLevel)Framework.GetModule<IProcedureManager>().GetProcedure<PlayOriginalLevel>()).Pause();
        public void Resume() => ((PauseOriginalLevel)Framework.GetModule<IProcedureManager>().GetProcedure<PauseOriginalLevel>()).Resume();
        public void Fail(string reason) { Failure = reason; Debug.LogError("FirstRep framework integration failed: " + reason); }
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
            if(ExitStatus==null || ExitStatus.Requested || ExitStatus.Completed)return;
            var current=Framework.GetModule<IProcedureManager>().CurrentProcedure as OriginalLevelProcedure;
            if(current!=null)current.Exit();else Lifetime.BeginExit();
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
