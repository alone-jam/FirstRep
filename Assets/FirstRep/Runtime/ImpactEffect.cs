using EjoyFramework.Core;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.Timer;
using EjoyFramework.Core.Unity;
using UnityEngine;
namespace FirstRep.Fps
{
    public sealed class ImpactEffect : EntityLogic
    {
        public ParticleSystem particles;ITimerManager timers;int expiry;
        protected override void OnInit(object data){base.OnInit(data);timers=Framework.GetModule<ITimerManager>();}
        protected override void OnShow(object data)
        {
            base.OnShow(data);var spawn=ActorSpawn.From(data);transform.SetPositionAndRotation(spawn.Position,spawn.Rotation);particles.Play(true);
            int id=Entity.Id;expiry=timers.AddTimer(5,()=>{expiry=0;Framework.GetModule<IEntityManager>().HideEntity(id);});
        }
        protected override void OnHide(bool shutdown,object data)
        {
            if(expiry!=0){timers.RemoveTimer(expiry);expiry=0;}particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);base.OnHide(shutdown,data);
        }
    }
}
