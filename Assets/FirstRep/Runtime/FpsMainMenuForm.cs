using EjoyFramework.Core.UI.Mvvm;
using EjoyFramework.Core.Unity;
namespace FirstRep.Fps
{
    public sealed class FpsMainMenuForm : MvvmView<FpsMainMenuViewModel>
    {
        protected override FpsMainMenuViewModel CreateViewModel(object data)
        {return new FpsMainMenuViewModel((SliceBootstrap)data);}
        protected override void OnViewModelUnbinding()=>ViewModel?.Release();
    }
    public sealed class FpsMainMenuViewModel : BindableObject
    {
        SliceBootstrap boot;
        public string Title=>"FIRSTREP";
        public string Description=>"LEVEL 01 · TARGET PRACTICE";
        public RelayCommand StartCommand { get; }
        public RelayCommand ExitCommand { get; }
        public FpsMainMenuViewModel(SliceBootstrap boot)
        {
            this.boot=boot;
            StartCommand=new RelayCommand(()=>this.boot.StartLevel(),()=>this.boot && !this.boot.ExitStatus.Requested);
            ExitCommand=new RelayCommand(()=>this.boot.Exit(),()=>this.boot && !this.boot.ExitStatus.Requested);
        }
        public void Release(){boot=null;StartCommand.RaiseCanExecuteChanged();ExitCommand.RaiseCanExecuteChanged();}
    }
}
