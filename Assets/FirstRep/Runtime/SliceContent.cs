using UnityEngine;
using System.Linq;

namespace FirstRep.Fps
{
    [CreateAssetMenu(menuName = "FirstRep/Reviewed FPS content")]
    public sealed class SliceContent : ScriptableObject
    {
        [Tooltip("Set only after reviewing source-vs-migrated visuals in Unity 6000.6.5f1.")]
        public bool artReviewComplete;
        public string[] levelScenes;
        public Vector3 spawnPosition;
        public float spawnYaw;
        public Vector3[] targetPositions;
        [Tooltip("Reviewed presentation prefabs made from original FPSSample art; no old game scripts.")]
        public GameObject firstPersonPrefab;
        public GameObject targetPrefab;
        public ParticleSystem impactPrefab;
        public AudioClip shotAudio;
        public bool IsReady => artReviewComplete && firstPersonPrefab && targetPrefab && impactPrefab &&
            shotAudio && levelScenes != null && levelScenes.Length > 0 &&
            levelScenes.All(path => !string.IsNullOrWhiteSpace(path)) &&
            levelScenes.Distinct().Count() == levelScenes.Length &&
            targetPositions != null && targetPositions.Length > 0;
    }
}
