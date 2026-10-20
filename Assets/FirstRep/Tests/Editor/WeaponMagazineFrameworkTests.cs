#if UNITY_EDITOR
using NUnit.Framework;

namespace FirstRep.Fps.Tests
{
    public sealed class WeaponMagazineFrameworkTests
    {
        [Test]
        public void RejectedCooldownDoesNotSpendAmmo()
        {
            var weapon=new WeaponMagazine(3,2);
            Assert.IsTrue(weapon.TryFire());
            Assert.IsFalse(weapon.TryFire());
            Assert.AreEqual(2,weapon.Rounds);
            weapon.Tick(0.119f);
            Assert.IsFalse(weapon.TryFire());
            weapon.Tick(0.002f);
            Assert.IsTrue(weapon.TryFire());
            Assert.AreEqual(1,weapon.Rounds);
        }
        [Test]
        public void ZeroElapsedTimeDoesNotAdvanceCooldown()
        {
            var weapon=new WeaponMagazine(3,2);
            Assert.IsTrue(weapon.TryFire());
            for(int i=0;i<30;i++)weapon.Tick(0);
            Assert.IsFalse(weapon.TryFire());
            Assert.AreEqual(2,weapon.Rounds);
        }
        [Test]
        public void ReloadTagBlocksAnOtherwiseReadyShotWithoutSpendingAmmo()
        {
            var weapon=new WeaponMagazine(3,2);
            Assert.IsTrue(weapon.TryFire());
            Assert.IsTrue(weapon.BeginReload());
            Assert.IsFalse(weapon.BeginReload());
            weapon.Tick(1);
            Assert.IsFalse(weapon.TryFire());
            Assert.AreEqual(2,weapon.Rounds);
            weapon.CompleteReload();
            Assert.AreEqual(3,weapon.Rounds);
            Assert.AreEqual(1,weapon.Reserve);
            Assert.IsFalse(weapon.Reloading);
            Assert.IsTrue(weapon.TryFire());
        }
        [Test]
        public void CancelledReloadCannotGrantAmmoOrKeepFireBlocked()
        {
            var weapon=new WeaponMagazine(3,8);
            weapon.TryFire();weapon.BeginReload();weapon.CancelReload();weapon.CompleteReload();
            Assert.AreEqual(2,weapon.Rounds);
            Assert.AreEqual(8,weapon.Reserve);
            weapon.Tick(0.13f);
            Assert.IsTrue(weapon.TryFire());
        }
        [Test]
        public void EmptyMagazineAndPartialReloadConserveFiniteReserve()
        {
            var weapon=new WeaponMagazine(3,2);
            for(int i=0;i<3;i++){Assert.IsTrue(weapon.TryFire());weapon.Tick(0.13f);}
            Assert.IsFalse(weapon.TryFire());Assert.AreEqual(0,weapon.Rounds);
            Assert.IsTrue(weapon.BeginReload());weapon.CompleteReload();
            Assert.AreEqual(2,weapon.Rounds);Assert.AreEqual(0,weapon.Reserve);
            Assert.IsFalse(weapon.BeginReload());
        }
        [Test]
        public void OwnersHaveIndependentAmmoCooldownAndReloadTags()
        {
            var a=new WeaponMagazine(3,2);var b=new WeaponMagazine(3,2);
            Assert.IsTrue(a.TryFire());Assert.IsTrue(b.TryFire());
            a.BeginReload();a.Tick(1);
            Assert.IsFalse(a.TryFire());Assert.IsFalse(b.TryFire());
            b.Tick(0.13f);Assert.IsTrue(b.TryFire());
            Assert.AreEqual(2,a.Rounds);Assert.AreEqual(1,b.Rounds);
        }
    }
}
#endif
