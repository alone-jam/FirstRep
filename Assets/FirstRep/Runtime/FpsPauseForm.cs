using EjoyFramework.Core.Unity;
namespace FirstRep.Fps
{
    public sealed class FpsPauseForm : MvvmView<FpsPauseViewModel>
    {
        protected override FpsPauseViewModel CreateViewModel(object data)
        {
            return new FpsPauseViewModel((SliceBootstrap)data);
        }
        protected override void OnViewModelUnbinding() => ViewModel?.Release();
    }
}
