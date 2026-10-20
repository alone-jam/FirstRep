using EjoyFramework.Core;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.Timer;
using EjoyFramework.Core.Unity;
using EjoyFramework.GamePlay.Units;
using UnityEngine;

namespace FirstRep.Fps
{
    public readonly struct TargetHit
    {
        public readonly bool Destroyed;
        public TargetHit(bool destroyed)=>Destroyed=destroyed;
    }
    public sealed class TargetPractice : EntityLogic
    {
        EventChannel<TargetHit> hits;
        ITimerManager timers;
        ActorSpawn spawn;
        int respawnTimer;
        public UnitModel Unit { get; private set; }
        public float Health => Unit?.Health ?? 0;

        protected override void OnInit(object data)
        {
            base.OnInit(data);
            timers=Framework.GetModule<ITimerManager>();
            hits=Framework.GetModule<IEventManager>().GetChannel<TargetHit>();
        }
        protected override void OnShow(object data)
        {
            base.OnShow(data); CancelRespawn(); UnsubscribeUnit();
            spawn=ActorSpawn.From(data);
            // A pooled prefab may return under a different framework Entity.Id.
            Unit=new UnitModel(Entity.Id, 1);
            Unit.ConfigureHealth(100);
            Unit.OnDamaged+=OnDamaged;
            Unit.OnDied+=OnDied;
            transform.SetPositionAndRotation(spawn.Position,spawn.Rotation);
        }
        public void Hit(float damage, int sourceId=-1)
        {
            if (Available) Unit.TakeDamage(damage,sourceId);
        }
        void OnDamaged(UnitModel unit,float applied,int sourceId) => hits.FireNow(new TargetHit(!unit.IsAlive));
        void OnDied(UnitModel unit,int sourceId)
        {
            if (ReferenceEquals(unit,Unit) && Available)
                Framework.GetModule<IEntityManager>().HideEntity(Entity.Id,true);
        }
        protected override void OnHide(bool shutdown,object data)
        {
            UnsubscribeUnit(); base.OnHide(shutdown,data);
            if(!shutdown && data is bool killed && killed)
            {
                int id=Entity.Id;
                respawnTimer=timers.AddTimer(4,()=>{
                    respawnTimer=0;
                    spawn.Bootstrap.Entities.ShowEntity(id,spawn.Bootstrap.content.TargetAssetPath,"Targets",0,spawn);
                });
            }
        }
        void CancelRespawn()
        {
            if(respawnTimer==0)return;
            timers.RemoveTimer(respawnTimer);respawnTimer=0;
        }
        void UnsubscribeUnit()
        {
            if(Unit==null)return;
            Unit.OnDamaged-=OnDamaged;Unit.OnDied-=OnDied;
        }
        protected override void OnRecycle(){CancelRespawn();UnsubscribeUnit();base.OnRecycle();}
    }
}
