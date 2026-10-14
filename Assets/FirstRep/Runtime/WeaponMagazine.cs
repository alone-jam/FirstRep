using System;

namespace FirstRep.Fps
{
    // Weapon-specific rules only. Scheduling belongs to EjoyFramework.ITimerManager.
    public sealed class WeaponMagazine
    {
        public int Capacity { get; }
        public int Rounds { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public WeaponMagazine(int capacity, int reserve)
        {
            if (capacity < 1 || reserve < 0) throw new ArgumentOutOfRangeException();
            Capacity = capacity; Rounds = capacity; Reserve = reserve;
        }
        public bool TryFire()
        {
            if (Reloading || Rounds == 0) return false;
            --Rounds; return true;
        }
        public bool BeginReload()
        {
            if (Reloading || Rounds == Capacity || Reserve == 0) return false;
            Reloading = true; return true;
        }
        public void CompleteReload()
        {
            if (!Reloading) return;
            int count = Math.Min(Capacity - Rounds, Reserve);
            Rounds += count; Reserve -= count; Reloading = false;
        }
        public void CancelReload() => Reloading = false;
    }
}
