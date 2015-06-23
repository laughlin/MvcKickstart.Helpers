using System.Collections.Generic;
using MvcKickstart.Infrastructure;
using NUnit.Framework;

namespace MvcKickstartHelpers.Test.Infastructure
{
    public class ConfigureCacheTests: TestBase
    {
        [Test]
        public void NullNodes_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(null));
        }

        [Test]
        public void EmptyList_ReturnTrue()
        {
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.ConfigureCache(new List<string>()));
        }


        [Test]
        public void ListWithNullEntries_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(new List<string>{"somestring", "some other string", null}));
        }

        [Test]
        public void ListWithEmptyEntries_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(new List<string> { "somestring", "some other string", "" }));
        }
    }
}
