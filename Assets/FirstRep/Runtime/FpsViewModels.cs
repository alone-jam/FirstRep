using EjoyFramework.Core.UI.Mvvm;
namespace FirstRep.Fps
{
    public sealed class FpsHudViewModel : BindableObject
    {
        FpsPlayer player;string header,crosshair;
        public string Header { get=>header;set=>SetField(ref header,value); }
        public string Crosshair { get=>crosshair;set=>SetField(ref crosshair,value); }
        public string Instructions => "WASD MOVE · SHIFT SPRINT · SPACE JUMP · LMB FIRE · RMB AIM · R RELOAD · ESC PAUSE";
        public FpsHudViewModel(FpsPlayer player){this.player=player;Refresh();}
        public void Refresh(){if(player){Header=player.HudText;Crosshair=player.HitMarker?"×":"+";}}
        public void Release(){player=null;}
    }
    public sealed class FpsPauseViewModel : BindableObject
    {
        SliceBootstrap bootstrap;
        public string Status => bootstrap && !string.IsNullOrEmpty(bootstrap.Failure) ? "LEVEL LOAD FAILED\n"+bootstrap.Failure : "PAUSED";
        public RelayCommand Resume { get; }
        public RelayCommand Exit { get; }
        public FpsPauseViewModel(SliceBootstrap bootstrap)
        {
            this.bootstrap=bootstrap;
            Resume=new RelayCommand(()=>this.bootstrap.Resume(),()=>this.bootstrap && this.bootstrap.IsPaused && string.IsNullOrEmpty(this.bootstrap.Failure) && !this.bootstrap.ExitStatus.Requested);
            Exit=new RelayCommand(()=>this.bootstrap.Exit(),()=>this.bootstrap && !this.bootstrap.ExitStatus.Requested);
        }
        public void Release(){bootstrap=null;Resume.RaiseCanExecuteChanged();Exit.RaiseCanExecuteChanged();}
    }
    static class FpsUiBindings
    {
        // Use the framework's typed accessor registry for Player/AOT as well as Editor.
        // Binding/subscription/unbinding and command execution remain framework-owned.
        public static void Register()
        {
            PropertyAccessor.Register(typeof(FpsHudViewModel),nameof(FpsHudViewModel.Header),o=>((FpsHudViewModel)o).Header,null);
            PropertyAccessor.Register(typeof(FpsHudViewModel),nameof(FpsHudViewModel.Crosshair),o=>((FpsHudViewModel)o).Crosshair,null);
            PropertyAccessor.Register(typeof(FpsHudViewModel),nameof(FpsHudViewModel.Instructions),o=>((FpsHudViewModel)o).Instructions,null);
            PropertyAccessor.Register(typeof(FpsPauseViewModel),nameof(FpsPauseViewModel.Status),o=>((FpsPauseViewModel)o).Status,null);
            PropertyAccessor.Register(typeof(FpsPauseViewModel),nameof(FpsPauseViewModel.Resume),o=>((FpsPauseViewModel)o).Resume,null);
            PropertyAccessor.Register(typeof(FpsPauseViewModel),nameof(FpsPauseViewModel.Exit),o=>((FpsPauseViewModel)o).Exit,null);
        }
    }
}
