using EjoyFramework.Core.Unity;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace FirstRep.Fps.Tests
{
    public sealed class UpstreamPointerInputTests : InputTestFixture
    {
        [Test] public void GitPackageMouseBindingsReachActualInputSystemDevice()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var helper=new NewInputSystemHelper();
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));InputSystem.Update();
                Assert.IsTrue(mouse.leftButton.isPressed,"Input fixture did not process the queued device state");Assert.IsTrue(helper.GetKey("Mouse0"));Assert.IsTrue(helper.GetKeyDown("Mouse0"));Assert.IsFalse(helper.GetKey("Mouse1"));
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));InputSystem.Update();
                Assert.IsTrue(helper.GetKeyUp("Mouse0"));Assert.IsTrue(helper.GetKey("Mouse1"));
                InputSystem.QueueStateEvent(mouse,new MouseState());InputSystem.Update();Assert.IsTrue(helper.GetKeyUp("Mouse1"));
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
    }
}
