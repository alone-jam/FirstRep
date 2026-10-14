using EjoyFramework.Core.Unity;
namespace FirstRep.Fps
{
    public sealed class FpsHudForm : MvvmView<FpsHudViewModel>
    {
        protected override FpsHudViewModel CreateViewModel(object data)
        {
            FpsUiBindings.Register();
            var bootstrap=(SliceBootstrap)data;
            var player=(bootstrap.Entities.GetEntity(1).Handle as UnityEngine.GameObject).GetComponent<FpsPlayer>();
            return new FpsHudViewModel(player);
        }
        public override void OnUpdate(float elapsed,float realElapsed) => ViewModel?.Refresh();
        protected override void OnViewModelUnbinding() => ViewModel?.Release();
    }
}
