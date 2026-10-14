using EjoyFramework.Core;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.Input;
using EjoyFramework.Core.Timer;
using UnityEngine;

namespace FirstRep.Fps
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsPlayer : MonoBehaviour
    {
        public Camera view;
        public FirstPersonPresentation presentation;
        public SliceContent content;
        public float speed = 5, sprintSpeed = 8, jumpHeight = 1.2f, sensitivity = 2;
        public LayerMask shotMask = ~((1 << 10) | (1 << 11) | (1 << 8) | (1 << 12));
        private CharacterController motor;
        private IInputManager input;
        private ITimerManager timers;
        private EventChannel<TargetHit> hits;
        private AudioSource audioSource;
        private WeaponMagazine magazine;
        private float pitch, verticalSpeed, nextShot, hitUntil;
        private int reloadTimer, kills;
        private bool paused;
        private void Start()
        {
            motor = GetComponent<CharacterController>();
            input = Framework.GetModule<IInputManager>(); timers = Framework.GetModule<ITimerManager>();
            hits = Framework.GetModule<IEventManager>().GetChannel<TargetHit>(); hits.Subscribe(OnHit);
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend = 0;
            magazine = new WeaponMagazine(30, 180);
            input.SetBinding("MoveX", InputBinding.Axis("Horizontal")); input.SetBinding("MoveZ", InputBinding.Axis("Vertical"));
            input.SetBinding("LookX", InputBinding.Axis("Mouse X")); input.SetBinding("LookY", InputBinding.Axis("Mouse Y"));
            input.SetBinding("Fire", InputBinding.Key("Mouse0")); input.SetBinding("Aim", InputBinding.Key("Mouse1"));
            input.SetBinding("Jump", InputBinding.Key("Space")); input.SetBinding("Sprint", InputBinding.Key("LeftShift"));
            input.SetBinding("Reload", InputBinding.Key("R")); input.SetBinding("Pause", InputBinding.Key("Escape"));
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        private void Update()
        {
            if (input == null || paused) return;
            if (input.GetButtonDown("Pause")) { Pause(); return; }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0, input.GetAxis("LookX") * sensitivity, 0);
                pitch = Mathf.Clamp(pitch - input.GetAxis("LookY") * sensitivity, -85, 85);
            }
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 move = Vector3.ClampMagnitude(new Vector3(input.GetAxis("MoveX"), 0, input.GetAxis("MoveZ")), 1);
            bool aiming = input.GetButton("Aim");
            float velocity = aiming ? speed * 0.55f : input.GetButton("Sprint") ? sprintSpeed : speed;
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (motor.isGrounded && input.GetButtonDown("Jump")) verticalSpeed = Mathf.Sqrt(jumpHeight * 2 * 20);
            verticalSpeed -= 20 * Time.deltaTime;
            motor.Move((transform.TransformDirection(move) * velocity + Vector3.up * verticalSpeed) * Time.deltaTime);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, aiming ? 45 : 75, 1 - Mathf.Exp(-12 * Time.deltaTime));
            if (transform.position.y < content.spawnPosition.y - 80) Respawn();
            if (input.GetButtonDown("Reload")) Reload();
            if (input.GetButton("Fire") && Time.time >= nextShot && magazine.TryFire())
            {
                nextShot = Time.time + 0.12f; Fire();
            }
        }
        private void Fire()
        {
            presentation.Fire(); audioSource.PlayOneShot(content.shotAudio);
            var ray = new Ray(view.transform.position, view.transform.forward);
            bool cameraHit = Physics.Raycast(ray, out var hit, 200, shotMask, QueryTriggerInteraction.Ignore);
            Vector3 end = cameraHit ? hit.point : ray.GetPoint(200);
            Vector3 muzzle = presentation.muzzle.position;
            Vector3 delta = end - muzzle;
            // A wall between muzzle and crosshair must absorb the shot.
            if (Physics.Raycast(muzzle, delta.normalized, out var obstruction, delta.magnitude, shotMask, QueryTriggerInteraction.Ignore))
            { hit = obstruction; cameraHit = true; }
            if (!cameraHit) return;
            hit.collider.GetComponentInParent<TargetPractice>()?.Hit(25);
            var fx = Instantiate(content.impactPrefab, hit.point + hit.normal * 0.005f, Quaternion.LookRotation(hit.normal));
            fx.Play(true);
            // Uses framework scheduling. No independent timer/coroutine service.
            timers.AddTimer(5, () => { if (fx) Destroy(fx.gameObject); });
        }
        private void Reload()
        {
            if (!magazine.BeginReload()) return;
            presentation.Reload();
            reloadTimer = timers.AddTimer(1.8f, () => { reloadTimer = 0; magazine.CompleteReload(); });
        }
        private void OnHit(in TargetHit hit) { hitUntil = Time.unscaledTime + 0.12f; if (hit.Destroyed) ++kills; }
        private void Respawn()
        {
            motor.enabled = false; transform.SetPositionAndRotation(SliceBootstrap.GroundPosition(content.spawnPosition), Quaternion.Euler(0, content.spawnYaw, 0));
            motor.enabled = true; verticalSpeed = 0; pitch = 0;
            view.transform.localRotation = Quaternion.identity;
        }
        private void Pause()
        {
            if (paused || input == null) return;
            paused = true; input.PushContext(InputContext.Menu); Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        private void Resume()
        {
            input.PopContext(); paused = false; Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) Pause(); }
        private void OnGUI()
        {
            if (magazine == null) return;
            GUI.Label(new Rect(20, 20, 650, 32), $"FIRSTREP • LEVEL 01    {magazine.Rounds:00} / {magazine.Reserve}    TARGETS {kills}");
            GUI.Label(new Rect(20, Screen.height - 40, 950, 30), "WASD MOVE · SHIFT SPRINT · SPACE JUMP · LMB FIRE · RMB AIM · R RELOAD · ESC PAUSE");
            if (paused)
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 90, Screen.height / 2 - 20, 180, 40), "Resume")) Resume();
            }
            else GUI.Label(new Rect(Screen.width / 2 - 7, Screen.height / 2 - 12, 30, 30), Time.unscaledTime < hitUntil ? "×" : "+");
            if (magazine.Reloading) GUI.Label(new Rect(Screen.width / 2 - 45, Screen.height / 2 + 30, 100, 25), "RELOADING");
        }
        private void OnDestroy()
        {
            if (reloadTimer != 0) timers?.RemoveTimer(reloadTimer);
            hits?.TryUnsubscribe(OnHit);
            if (paused) input?.PopContext();
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
