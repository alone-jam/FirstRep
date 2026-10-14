using System;
using EjoyFramework.Core;
using EjoyFramework.Core.Timer;
using EjoyFramework.Core.Input;
using EjoyFramework.GamePlay.Attributes;
using FirstRep.Fps;
class Program {
 static int checks;
 static void Check(bool condition,string name){ if(!condition) throw new Exception(name); ++checks; Console.WriteLine("PASS "+name); }
 static void Main(){
 var m=new WeaponMagazine(3,2);
 Check(m.TryFire()&&m.TryFire()&&m.TryFire()&&!m.TryFire(),"magazine dry-fire prevention");
 Check(m.BeginReload()&&!m.BeginReload()&&!m.TryFire(),"reload excludes duplicate reload and fire");
 Framework.MarkMainThread(); EjoyFramework.Core.Generated.FrameworkModuleRegistrations.RegisterAll(); var timers=Framework.GetModule<ITimerManager>();
 timers.AddTimer(1,m.CompleteReload); Framework.Update(0.5f,0.5f);Check(m.Reloading,"framework timer does not complete early");
 Framework.Update(0.6f,0.6f); Check(m.Rounds==2&&m.Reserve==0&&!m.Reloading,"reload completes via real Ejoy timer conserving ammo");
 Check(!m.BeginReload(),"empty reserve refuses reload");
 var n=new WeaponMagazine(3,8); n.TryFire();n.BeginReload();n.CancelReload();n.CompleteReload();Check(n.Rounds==2&&n.Reserve==8,"cancelled reload does not grant ammunition");
 var attributes=new AttributeSet();var hp=attributes.Add("Health",100);hp.BaseValue=Math.Max(0,hp.CurrentValue-25);Check(hp.CurrentValue==75,"real Ejoy health attribute integration");
 Framework.Shutdown();Console.WriteLine("Passed "+checks+" pure .NET checks; NOT Unity import/play-mode/graphics validation.");
 }
}
