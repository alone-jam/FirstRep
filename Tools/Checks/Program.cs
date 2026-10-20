using System;
using EjoyFramework.Core;
using EjoyFramework.Core.Timer;
using EjoyFramework.Core.Input;
using EjoyFramework.GamePlay.Units;
using FirstRep.Fps;
class Program {
 static int checks;
 static void Check(bool condition,string name){ if(!condition) throw new Exception(name); ++checks; Console.WriteLine("PASS "+name); }
 static void Main(){
 var m=new WeaponMagazine(3,2);
 Check(m.TryFire()&&!m.TryFire()&&m.Rounds==2,"real ability cooldown rejects without spending ammunition");
 m.Tick(0.13f);Check(m.TryFire(),"owner tick releases framework cooldown");m.Tick(0.13f);Check(m.TryFire()&&!m.TryFire(),"magazine dry-fire prevention");
 Check(m.BeginReload()&&!m.BeginReload()&&!m.TryFire(),"reload excludes duplicate reload and fire");
 Framework.MarkMainThread(); EjoyFramework.Core.Generated.FrameworkModuleRegistrations.RegisterAll(); var timers=Framework.GetModule<ITimerManager>();
 timers.AddTimer(1,m.CompleteReload); Framework.Update(0.5f,0.5f);Check(m.Reloading,"framework timer does not complete early");
 Framework.Update(0.6f,0.6f); Check(m.Rounds==2&&m.Reserve==0&&!m.Reloading,"reload completes via real Ejoy timer conserving ammo");
 Check(!m.BeginReload(),"empty reserve refuses reload");
 var n=new WeaponMagazine(3,8); n.TryFire();n.BeginReload();n.CancelReload();n.CompleteReload();Check(n.Rounds==2&&n.Reserve==8,"cancelled reload does not grant ammunition");
 var unit=new UnitModel(100,1);unit.ConfigureHealth(100);var damage=unit.TakeDamage(25,1);Check(unit.Health==75&&damage.Applied==25,"real Ejoy UnitModel damage integration");
 Framework.Shutdown();Console.WriteLine("Passed "+checks+" pure .NET checks; NOT Unity import/play-mode/graphics validation.");
 }
}
