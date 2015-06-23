using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcKickstart.Infrastructure;

namespace MvcKickstartHelpers.Test.Infastructure
{
    [TestClass]
    public class ConfigureCacheTests
    {
        [TestMethod]
        public void NullNodes_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(null));
        }

        [TestMethod]
        public void EmptyList_ReturnTrue()
        {
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.ConfigureCache(new List<string>()));
        }


        [TestMethod]
        public void ListWithNullEntries_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(new List<string>{"somestring", "some other string", null}));
        }

        [TestMethod]
        public void ListWithEmptyEntries_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.ConfigureCache(new List<string> { "somestring", "some other string", "" }));
        }
    }
}
