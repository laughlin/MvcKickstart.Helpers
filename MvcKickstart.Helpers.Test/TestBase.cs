using NUnit.Framework;

namespace MvcKickstartHelpers.Test
{
    public abstract class TestBase
    {
        [TestFixtureSetUp]
        public virtual void SetupFixture()
        {
        }

        [TestFixtureTearDown]
        public virtual void TearDownFixture()
        {
        }

        [SetUp]
        public virtual void Setup()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
        }
    }

}
