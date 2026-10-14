using System;
using System.Collections.Generic;
using EjoyFramework.Core;
using EjoyFramework.Core.Coroutines;
using EjoyFramework.Core.Input;
using EjoyFramework.Core.Scene;
using EjoyFramework.Core.Unity;
using UnityEngine;

namespace FirstRep.Fps
{
    public sealed class SliceBootstrap : MonoBehaviour
    {
        public SliceContent content;
        private ISceneManager scenes;
        private readonly HashSet<string> loaded = new HashSet<string>();
        private bool started;
        private bool failed;
        private void Awake()
        {
            failed = true;
            if (!content || !content.IsReady)
                throw new InvalidOperationException("Original FPSSample art migration is not approved. Run the migration validation first; no fallback art is supplied.");
            // The only framework lifetime driver is the upstream BaseComponent.
            gameObject.AddComponent<BaseComponent>();
            var input = gameObject.AddComponent<InputComponent>();
            // Upstream #1: use its existing fallback, not a custom input framework.
            input.SetHelperOverride(new LegacyInputHelper());
            scenes = Framework.GetModule<ISceneManager>();
            scenes.SetHelper(new DefaultSceneHelper(Framework.GetModule<ICoroutineManager>(), null));
            scenes.LoadSceneSuccess += OnLoaded; scenes.LoadSceneFailure += OnFailed;
            failed = false;
        }
        private void Start()
        {
            if (failed || scenes == null) return;
            foreach (string scene in content.levelScenes) scenes.LoadScene(scene, 0, this);
        }
        private void OnLoaded(object sender, LoadSceneSuccessEventArgs e)
        {
            if (!ReferenceEquals(e.UserData, this) || failed || started ||
                Array.IndexOf(content.levelScenes, e.SceneAssetName) < 0 ||
                !loaded.Add(e.SceneAssetName) || loaded.Count != content.levelScenes.Length) return;
            started = true;
            var player = new GameObject("FirstRep Player"); player.layer = 10;
            player.transform.SetPositionAndRotation(GroundPosition(content.spawnPosition), Quaternion.Euler(0, content.spawnYaw, 0));
            var motor = player.AddComponent<CharacterController>(); motor.height = 1.8f;
            motor.center = Vector3.up * 0.9f; motor.radius = 0.3f; motor.stepOffset = 0.3f;
            var cameraObject = new GameObject("Player Camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false); cameraObject.transform.localPosition = Vector3.up * 1.65f;
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = 0.03f; camera.fieldOfView = 75; camera.cullingMask = ~((1<<8)|(1<<12)|(1<<20)|(1<<21)|(1<<26));
            PresentationSettings.Configure(camera);
            cameraObject.AddComponent<AudioListener>();
            var art = Instantiate(content.firstPersonPrefab, cameraObject.transform, false);
            foreach (var t in art.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 11;
            var presentation = art.GetComponentInChildren<FirstPersonPresentation>();
            if (!presentation || !presentation.muzzle) throw new InvalidOperationException("Reviewed first-person prefab lacks muzzle/presentation binding.");
            var controller = player.AddComponent<FpsPlayer>(); controller.view = camera;
            controller.content = content; controller.presentation = presentation;
            foreach (Vector3 position in content.targetPositions)
            {
                var grounded = GroundPosition(position);
                var facing = player.transform.position - grounded; facing.y = 0;
                var target = Instantiate(content.targetPrefab, grounded, Quaternion.LookRotation(facing));
                if (!target.GetComponent<TargetPractice>()) target.AddComponent<TargetPractice>();
            }
        }
        public static Vector3 GroundPosition(Vector3 position)
        {
            int mask = ~((1 << 10) | (1 << 11) | (1 << 8) | (1 << 12) | (1 << 25) | (1 << 26));
            return Physics.Raycast(position + Vector3.up * 2, Vector3.down, out var floor, 20, mask, QueryTriggerInteraction.Ignore)
                ? floor.point + Vector3.up * 0.05f : position;
        }
        private void OnFailed(object sender, LoadSceneFailureEventArgs e)
        {
            if (!ReferenceEquals(e.UserData, this)) return;
            failed = true; Debug.LogError("FPSSample scene load failed: " + e.SceneAssetName + ": " + e.ErrorMessage);
        }
        private void OnDestroy()
        {
            if (scenes == null) return;
            scenes.LoadSceneSuccess -= OnLoaded; scenes.LoadSceneFailure -= OnFailed;
        }
    }
}
