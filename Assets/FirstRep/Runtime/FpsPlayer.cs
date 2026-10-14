using EjoyFramework.Core;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.Input;
using EjoyFramework.Core.Timer;
using UnityEngine;
using EjoyFramework.Core.Unity;
using EjoyFramework.GamePlay.Unity.ThreeC;
using EjoyFramework.GamePlay.ThreeC;

namespace FirstRep.Fps
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsPlayer : EntityLogic
    {
        public Camera view;
        public FirstPersonPresentation presentation;
        public SliceContent content;
        public float speed = 5, sprintSpeed = 8, jumpHeight = 1.2f, sensitivity = 0.2f;
        public LayerMask shotMask = ~((1 << 10) | (1 << 11) | (1 << 8) | (1 << 12));
        private CharacterController motor;
        private IInputManager input;
        private ITimerManager timers;
        private EventChannel<TargetHit> hits;
        private SliceBootstrap bootstrap;
        private CharacterControllerStepper stepper;
        private ThirdPersonMotor locomotion;
        private uint controlSequence;
        private WeaponMagazine magazine;
        private float pitch, nextShot, hitUntil;
        private int reloadTimer, kills;
        public bool IsPaused => bootstrap && bootstrap.IsPaused;
        public string HudText => $"FIRSTREP • LEVEL 01    {magazine.Rounds:00} / {magazine.Reserve}    TARGETS {kills}{(magazine.Reloading ? "    RELOADING" : "")}";
        public bool HitMarker => Time.unscaledTime < hitUntil;
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            motor = GetComponent<CharacterController>(); stepper = new CharacterControllerStepper(motor); locomotion = new ThirdPersonMotor(speed,sprintSpeed,24,30,jumpHeight,20); PresentationSettings.Configure(view);
            input = Framework.GetModule<IInputManager>(); timers = Framework.GetModule<ITimerManager>();
            hits = Framework.GetModule<IEventManager>().GetChannel<TargetHit>();
            magazine = new WeaponMagazine(30, 180);
            input.SetBinding("MoveX", InputBinding.Axis("Horizontal")); input.SetBinding("MoveZ", InputBinding.Axis("Vertical"));
            input.SetBinding("LookX", InputBinding.Axis("Mouse X")); input.SetBinding("LookY", InputBinding.Axis("Mouse Y"));
            input.SetBinding("Fire", InputBinding.Key("Mouse0")); input.SetBinding("Aim", InputBinding.Key("Mouse1"));
            input.SetBinding("Jump", InputBinding.Key("Space")); input.SetBinding("Sprint", InputBinding.Key("LeftShift"));
            input.SetBinding("Reload", InputBinding.Key("R")); input.SetBinding("Pause", InputBinding.Key("Escape"));
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        protected override void OnShow(object userData)
        {
            base.OnShow(userData); var spawn=ActorSpawn.From(userData); bootstrap=spawn.Bootstrap;content=bootstrap.content;
            transform.SetPositionAndRotation(spawn.Position,spawn.Rotation); magazine=new WeaponMagazine(30,180); pitch=nextShot=0;kills=0;locomotion.Reset();
            hits.Subscribe(OnHit);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }
        protected override void OnUpdate(float elapseSeconds,float realElapseSeconds)
        {
            if (input == null || IsPaused || elapseSeconds <= 0) return;
            if (input.GetButtonDown("Pause")) { Pause(); return; }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0, input.GetAxis("LookX") * sensitivity, 0);
                pitch = Mathf.Clamp(pitch - input.GetAxis("LookY") * sensitivity, -85, 85);
            }
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 move = Vector3.ClampMagnitude(new Vector3(input.GetAxis("MoveX"), 0, input.GetAxis("MoveZ")), 1);
            bool aiming = input.GetButton("Aim");
            // Reuse the framework motor's acceleration, gravity and jump integration;
            // FPS chooses its input scaling and camera-facing convention only.
            float inputScale = aiming ? 0.55f : 1f;
            var control=new PlayerControlFrame(controlSequence++,move.x*inputScale,move.z*inputScale,0,0,0,input.GetButtonDown("Jump"),!aiming && input.GetButton("Sprint"));
            var feedback=new CharacterMotorFeedback(motor.isGrounded);
            var movement=locomotion.Step(in control,in feedback,transform.eulerAngles.y,elapseSeconds);
            stepper.Move(transform.position,new Vector3(movement.Displacement.X,movement.Displacement.Y,movement.Displacement.Z),elapseSeconds);
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
            presentation.Fire(); bootstrap.Sounds.PlaySound(content.ShotAudioPath,"Weapons");
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
            bootstrap.ShowImpact(hit.point + hit.normal * 0.005f, Quaternion.LookRotation(hit.normal));
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
            stepper.RestorePosition(SliceBootstrap.GroundPosition(content.spawnPosition)); transform.rotation=Quaternion.Euler(0,content.spawnYaw,0); locomotion.Reset(); pitch = 0;
            view.transform.localRotation = Quaternion.identity;
        }
        private void Pause() => bootstrap?.Pause();
        private void Resume() => bootstrap?.Resume();
        private void OnApplicationFocus(bool focus) { if (!focus) Pause(); }
        protected override void OnHide(bool isShutdown,object userData)
        {
            if(reloadTimer!=0){timers.RemoveTimer(reloadTimer);reloadTimer=0;}
            magazine?.CancelReload();hits?.Unsubscribe(OnHit);base.OnHide(isShutdown,userData);
        }
        private void OnDestroy()
        {
            if (reloadTimer != 0) timers?.RemoveTimer(reloadTimer);
            hits?.TryUnsubscribe(OnHit);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
