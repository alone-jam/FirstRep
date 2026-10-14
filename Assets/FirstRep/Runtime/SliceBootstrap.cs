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
        public AssetBatch ContentBatch { get; set; }
        public bool IsPaused => Base && Base.IsGamePaused;
        public string Failure { get; private set; }
        private int nextEffectId = 10000;
        void Awake()
        {
            if (!content || !content.IsReady) throw new InvalidOperationException("Reviewed original content is missing; no replacement art is supplied.");
            Base = gameObject.AddComponent<BaseComponent>();
            gameObject.AddComponent<InputComponent>(); // Default upstream Input System backend, now Mouse0/1 capable.
            Resources = gameObject.AddComponent<ResourceComponent>();
            Entities = gameObject.AddComponent<EntityComponent>();
            Entities.AddEntityGroup("Players", 60, 1, 60, 0);
            Entities.AddEntityGroup("Targets", 60, content.targetPositions.Length, 60, 0);
            Entities.AddEntityGroup("Effects", 60, 32, 60, 0);
            Sounds = gameObject.AddComponent<SoundComponent>(); Sounds.AddSoundGroup("Weapons");
            UI = Framework.GetModule<IUIManager>();
            UI.SetUIFormHelper(gameObject.AddComponent<DefaultUIFormHelper>());
            var group = new GameObject("FirstRep UI group").AddComponent<UIGroupCanvasHelper>();
            group.transform.SetParent(transform, false);
            group.Configure("FirstRep", 0, new Vector2(1280,720), .5f);
            UI.AddUIGroup("FirstRep", 0, group);
            Scenes = Framework.GetModule<ISceneManager>();
            Scenes.SetHelper(new DefaultSceneHelper(Framework.GetModule<ICoroutineManager>(), null));
        }
        void Start()
        {
            var procedures = Framework.GetModule<IProcedureManager>();
            procedures.Initialize(Framework.GetModule<IFsmManager>(), new LoadOriginalLevel(this), new PlayOriginalLevel(this), new PauseOriginalLevel(this), new FailedOriginalLevel(this));
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
        void OnDestroy()
        {
            ContentBatch?.Release(); ContentBatch=null;
            Framework.Shutdown(); // End the one owned framework session; modules release pools/resources themselves.
        }
    }
    public sealed class ActorSpawn
    {
        public readonly SliceBootstrap Bootstrap; public readonly Vector3 Position; public readonly Quaternion Rotation;
        public ActorSpawn(SliceBootstrap bootstrap, Vector3 position, Quaternion rotation) { Bootstrap=bootstrap;Position=position;Rotation=rotation; }
        public static ActorSpawn From(object data) => (ActorSpawn)((EntityData)data).UserData;
    }
}
