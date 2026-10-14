using EjoyFramework.Core;
using EjoyFramework.Core.Unity;
using NUnit.Framework;
using UnityEngine;

namespace FirstRep.Fps.Tests
{
    public sealed class EntityHelperCompatibilityTests
    {
        [Test]
        public void ExplicitFrameworkIdSurvivesUnityEntityIdMigration()
        {
            var instance = new GameObject("Entity helper test");
            try
            {
                var entity = new DefaultEntityHelper(null).CreateEntity(instance, null,
                    new EntityData { EntityId = 42017, AssetName = "test/original" });
                Assert.That(entity.Id, Is.EqualTo(42017));
                Assert.That(entity.EntityAssetName, Is.EqualTo("test/original"));
                Assert.That(entity.Handle, Is.SameAs(instance));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void MissingExplicitIdFailsBeforeAttachingLogic()
        {
            var instance = new GameObject("Entity helper missing ID test");
            try
            {
                Assert.Throws<FrameworkException>(() => new DefaultEntityHelper(null).CreateEntity(instance, null, null));
                Assert.That(instance.GetComponent<EntityLogic>(), Is.Null);
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
