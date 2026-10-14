using EjoyFramework.Core;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.Timer;
using EjoyFramework.GamePlay.Attributes;
using UnityEngine;

namespace FirstRep.Fps
{
    public readonly struct TargetHit
    {
        public readonly bool Destroyed;
        public TargetHit(bool destroyed) => Destroyed = destroyed;
    }
    public sealed class TargetPractice : MonoBehaviour
    {
        private GameplayAttribute health;
        private EventChannel<TargetHit> hits;
        private ITimerManager timers;
        private int respawnTimer;
        private Renderer[] renderers;
        private Collider[] colliders;
        private bool[] rendererStates;
        private bool[] colliderStates;
        private void Awake()
        {
            health = new AttributeSet().Add("Health", 100);
            timers = Framework.GetModule<ITimerManager>();
            hits = Framework.GetModule<IEventManager>().GetChannel<TargetHit>();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            rendererStates = new bool[renderers.Length];
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < renderers.Length; ++i) rendererStates[i] = renderers[i].enabled;
            for (int i = 0; i < colliders.Length; ++i) colliderStates[i] = colliders[i].enabled;
        }
        public void Hit(float damage)
        {
            if (health.CurrentValue <= 0 || damage <= 0) return;
            health.BaseValue = Mathf.Max(0, health.CurrentValue - damage);
            bool dead = health.CurrentValue <= 0;
            hits.FireNow(new TargetHit(dead));
            if (!dead) return;
            SetVisible(false);
            respawnTimer = timers.AddTimer(4, () => { respawnTimer = 0; health.BaseValue = 100; SetVisible(true); });
        }
        private void SetVisible(bool visible)
        {
            for (int i = 0; i < renderers.Length; ++i)
                if (renderers[i]) renderers[i].enabled = visible && rendererStates[i];
            for (int i = 0; i < colliders.Length; ++i)
                if (colliders[i]) colliders[i].enabled = visible && colliderStates[i];
        }
        private void OnDestroy() { if (respawnTimer != 0) timers?.RemoveTimer(respawnTimer); }
    }
}
