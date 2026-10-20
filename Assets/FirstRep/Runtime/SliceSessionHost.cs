using System;
using System.Collections.Generic;
using EjoyFramework.Core;
using EjoyFramework.Core.Resource;
using EjoyFramework.Core.Scene;
using EjoyFramework.Core.UI;
using EjoyFramework.Core.Unity;
using UnityEngine;
namespace FirstRep.Fps
{
    public sealed class SliceExitStatus
    {
        internal GameObject services;
        public bool Requested { get; internal set; }
        public bool ScenesReleased { get; internal set; }
        public bool FrameworkClosed { get; internal set; }
        public string Error { get; internal set; }
        public bool Completed => FrameworkClosed && !services;
    }
    // Consumer ownership boundary, not a scene loader or framework tick driver.
    // The persistent BaseComponent stays alive until every upstream SceneLease settles.
    public sealed class SliceSessionHost : MonoBehaviour
    {
        public SliceBootstrap Bootstrap { get; set; }
        readonly List<SceneLease> leases=new List<SceneLease>();
        public SliceExitStatus ExitStatus { get; private set; }
        public IReadOnlyList<SceneLease> Scenes => leases;
        public BaseComponent Base { get; set; }
        public EntityComponent Entities { get; set; }
        public SoundComponent Sounds { get; set; }
        public IUIManager UI { get; set; }
        public ISceneManager SceneManager { get; set; }
        public AssetBatch ContentBatch { get; set; }
        public void Initialize(){ExitStatus=new SliceExitStatus{services=gameObject};}
        public void AcquireScenes(string[] paths)
        {
            if(ExitStatus.Requested)throw new InvalidOperationException("Session is exiting");
            foreach(var path in paths)leases.Add(new SceneLease(SceneManager,path));
        }
        public void BeginExit()
        {
            if(ExitStatus.Requested)return;ExitStatus.Requested=true;
            Base.PauseGame();UI.CloseAllLoadingUIForms();UI.CloseAllLoadedUIForms();
            Entities.HideAllLoadingEntities();Entities.HideAllLoadedEntities();Sounds.StopAllLoadedSounds();
            if(ContentBatch!=null && !ContentBatch.IsDone)ContentBatch.Cancel();
            foreach(var lease in leases)lease.Dispose();
        }
        public void RetrySceneRelease()
        {
            foreach(var lease in leases)if(lease.State==SceneLeaseState.ReleaseFailed)lease.Dispose();
            ExitStatus.Error=null;
        }
        void LateUpdate()
        {
            if(!ExitStatus.Requested || ExitStatus.FrameworkClosed)return;
            foreach(var lease in leases)
            {
                if(lease.State==SceneLeaseState.ReleaseFailed && ExitStatus.Error==null)
                {
                    ExitStatus.Error=lease.Error??"Scene release failed";Debug.LogError("FirstRep scene exit failed; framework retained for retry: "+ExitStatus.Error);
                }
                if(!lease.IsTerminal)return;
            }
            ExitStatus.ScenesReleased=true;
            ContentBatch?.Release();ContentBatch=null;
            Framework.Shutdown();ExitStatus.FrameworkClosed=true;
            // Let native destruction and ComponentRegistry unregistration finish before reentry.
            Destroy(gameObject);
        }
    }
}
