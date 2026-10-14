using UnityEngine;

namespace FirstRep.Fps
{
    // Bind in the reviewed art prefab. All meshes, clips, textures and particles
    // must originate in FPSSample; this component has no generated-art fallback.
    public sealed class FirstPersonPresentation : MonoBehaviour
    {
        public Transform muzzle;
        public ParticleSystem muzzleFlash;
        public Animator arms;
        public Animator weapon;
        public Transform cameraAnchor;
        public Transform itemAnchor;
        public Transform weaponRoot;
        public void AlignOriginalRig()
        {
            if (arms && cameraAnchor) arms.transform.position += transform.position - cameraAnchor.position;
            if (weaponRoot && itemAnchor) weaponRoot.SetPositionAndRotation(itemAnchor.position, itemAnchor.rotation);
        }
        private void LateUpdate() { AlignOriginalRig(); }
        public string fireState = "Fire";
        public string reloadState = "Reload";
        public void Fire()
        {
            if (muzzleFlash) muzzleFlash.Play(true);
            Play(arms, fireState); Play(weapon, fireState);
        }
        public void Reload() { Play(arms, reloadState); Play(weapon, reloadState); }
        private static void Play(Animator animator, string state)
        {
            if (animator && animator.runtimeAnimatorController && animator.HasState(0, Animator.StringToHash(state)))
                animator.CrossFadeInFixedTime(state, 0.03f);
        }
    }
}
