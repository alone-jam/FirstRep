using EjoyFramework.Core;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.Timer;
using EjoyFramework.Core.Unity;
using EjoyFramework.GamePlay.Attributes;
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
        GameplayAttribute health;EventChannel<TargetHit> hits;ITimerManager timers;ActorSpawn spawn;int respawnTimer;
        protected override void OnInit(object data)
        {
            base.OnInit(data);health=new AttributeSet().Add("Health",100);
            timers=Framework.GetModule<ITimerManager>();hits=Framework.GetModule<IEventManager>().GetChannel<TargetHit>();
        }
        protected override void OnShow(object data)
        {
            base.OnShow(data);spawn=ActorSpawn.From(data);health.BaseValue=100;
            transform.SetPositionAndRotation(spawn.Position,spawn.Rotation);
        }
        public void Hit(float damage)
        {
            if(!Available || health.CurrentValue<=0 || damage<=0)return;
            health.BaseValue=Mathf.Max(0,health.CurrentValue-damage);bool dead=health.CurrentValue<=0;
            hits.FireNow(new TargetHit(dead));
            if(dead)Framework.GetModule<IEntityManager>().HideEntity(Entity.Id,true);
        }
        protected override void OnHide(bool shutdown,object data)
        {
            base.OnHide(shutdown,data);
            if(!shutdown && data is bool killed && killed)
            {
                int id=Entity.Id;respawnTimer=timers.AddTimer(4,()=>{respawnTimer=0;spawn.Bootstrap.Entities.ShowEntity(id,spawn.Bootstrap.content.TargetAssetPath,"Targets",0,spawn);});
            }
        }
        protected override void OnRecycle(){if(respawnTimer!=0){timers.RemoveTimer(respawnTimer);respawnTimer=0;}base.OnRecycle();}
    }
}
