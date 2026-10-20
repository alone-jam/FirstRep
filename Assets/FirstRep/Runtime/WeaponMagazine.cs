using System;
using EjoyFramework.GamePlay.Abilities;
using EjoyFramework.GamePlay.Attributes;

namespace FirstRep.Fps
{
    // FPS magazine rules compose the real framework ability/cost/cooldown pipeline.
    // EntityLogic advances Tick; ITimerManager owns the reload completion schedule.
    public sealed class WeaponMagazine
    {
        const string FireAbility = "FirstRep.Fire";
        const string AmmoAttribute = "Ammo";
        const string ReloadingTag = "FirstRep.Weapon.Reloading";
        static readonly AbilityDefinition FireDefinition = AbilityDefinition.Create(FireAbility)
            .WithCooldown(0.12f).AddCost(AmmoAttribute, 1).BlockedBy(ReloadingTag).Build();
        readonly AbilitySystem abilities;
        readonly GameplayAttribute ammo;
        public int Capacity { get; }
        public int Rounds => (int)ammo.CurrentValue;
        public int Reserve { get; private set; }
        public bool Reloading => abilities.Tags.HasTag(ReloadingTag);

        public WeaponMagazine(int capacity, int reserve)
        {
            if (capacity < 1 || reserve < 0) throw new ArgumentOutOfRangeException();
            Capacity = capacity; Reserve = reserve;
            var attributes = new AttributeSet();
            ammo = attributes.Add(AmmoAttribute, capacity);
            abilities = new AbilitySystem(attributes);
            abilities.GrantAbility(FireDefinition);
        }
        public void Tick(float elapsedSeconds) => abilities.Tick(elapsedSeconds);
        public bool TryFire() => abilities.TryActivate(FireAbility) == AbilityActivationResult.Success;
        public bool BeginReload()
        {
            if (Reloading || Rounds == Capacity || Reserve == 0) return false;
            abilities.Tags.Add(ReloadingTag); return true;
        }
        public void CompleteReload()
        {
            if (!Reloading) return;
            int count = Math.Min(Capacity - Rounds, Reserve);
            ammo.BaseValue += count; Reserve -= count; CancelReload();
        }
        public void CancelReload() => abilities.Tags.Remove(ReloadingTag);
    }
}
