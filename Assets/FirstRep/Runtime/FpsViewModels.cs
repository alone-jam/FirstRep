using EjoyFramework.Core;
using EjoyFramework.Core.Event;
using EjoyFramework.Core.UI.Mvvm;
namespace FirstRep.Fps
{
    public sealed class FpsHudViewModel : BindableObject
    {
        FpsPlayer player;string header,crosshair;
        readonly int entityId;
        readonly EventChannel<PlayerHudChanged> changes;
        readonly EventChannelHandler<PlayerHudChanged> changed;
        public string Header { get=>header;set=>SetField(ref header,value); }
        public string Crosshair { get=>crosshair;set=>SetField(ref crosshair,value); }
        public string Instructions => "WASD MOVE · SHIFT SPRINT · SPACE JUMP · LMB FIRE · RMB AIM · R RELOAD · ESC PAUSE";
        public FpsHudViewModel(FpsPlayer player)
        {
            this.player=player;entityId=player.Entity.Id;
            changes=Framework.GetModule<IEventManager>().GetChannel<PlayerHudChanged>();
            changed=OnChanged;changes.Subscribe(changed);
            Apply(player.HudState);
        }
        void OnChanged(in PlayerHudChanged state)
        {
            if(ReferenceEquals(state.Player,player) && state.EntityId==entityId)Apply(state);
        }
        void Apply(in PlayerHudChanged state){Header=state.Header;Crosshair=state.HitMarker?"×":"+";}
        public void Release(){changes.TryUnsubscribe(changed);player=null;}
    }
    public sealed class FpsPauseViewModel : BindableObject
    {
        SliceBootstrap bootstrap;
        public string Status => bootstrap && !string.IsNullOrEmpty(bootstrap.Failure) ? "LEVEL LOAD FAILED\n"+bootstrap.Failure : "PAUSED";
        public bool ShowResume => bootstrap && string.IsNullOrEmpty(bootstrap.Failure);
        public bool CanRetry => bootstrap && bootstrap.CanRetry;
        public RelayCommand ResumeCommand { get; }
        public bool ShowMainMenu => bootstrap && bootstrap.CanReturnToMenu;
        public RelayCommand MainMenuCommand { get; }
        public RelayCommand RetryCommand { get; }
        public RelayCommand ExitCommand { get; }
        public FpsPauseViewModel(SliceBootstrap bootstrap)
        {
            this.bootstrap=bootstrap;
            ResumeCommand=new RelayCommand(()=>this.bootstrap.Resume(),()=>this.bootstrap && this.bootstrap.IsPaused && string.IsNullOrEmpty(this.bootstrap.Failure) && !this.bootstrap.ExitStatus.Requested);
            MainMenuCommand=new RelayCommand(()=>this.bootstrap.ReturnToMenu(),()=>ShowMainMenu);
            RetryCommand=new RelayCommand(()=>this.bootstrap.Retry(),()=>CanRetry);
            ExitCommand=new RelayCommand(()=>this.bootstrap.Exit(),()=>this.bootstrap && !this.bootstrap.ExitStatus.Requested);
        }
        public void Release(){bootstrap=null;ResumeCommand.RaiseCanExecuteChanged();RetryCommand.RaiseCanExecuteChanged();MainMenuCommand.RaiseCanExecuteChanged();ExitCommand.RaiseCanExecuteChanged();}
    }
}
