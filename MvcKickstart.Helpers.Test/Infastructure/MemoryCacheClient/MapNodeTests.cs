using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcKickstart.Infrastructure;

namespace MvcKickstartHelpers.Test.Infastructure
{
    [TestClass]
    public class MapNodeTests
    {
        [TestMethod]
        public void ZeroParts_ReturnFalse()
        {
            var inputs = new string[] { };
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.MapNode(inputs, string.Empty));
        }

        [TestMethod]
        public void OnePart_ReturnFalse()
        {
            var inputs = new string[]{"somestring"};

            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.MapNode(inputs, string.Empty));
        }

        [TestMethod]
        public void MachineNameNoOverride_ReturnFalse()
        {
            var inputs = new string[] { Environment.MachineName, "someurl" };
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.MapNode(inputs, string.Empty));
        }

        [TestMethod]
        public void MachineNameOverride_ReturnTrue()
        {
            var inputs = new string[] { Environment.MachineName, "someurl" };
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.MapNode(inputs, string.Empty, true));
            Assert.IsTrue(subject.BroadcastNodes.Contains("http://someurl"));
        }

        [TestMethod]
        public void NoneMachineName_ReturnTrue()
        {
            var inputs = new string[] { "somemachinename", "someurl" };
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.MapNode(inputs, string.Empty, true));
            Assert.IsTrue(subject.BroadcastNodes.Contains("http://someurl"));
        }

        [TestMethod]
        public void NodeNull_ReturnFalse()
        {
            var inputs = new string[] { "somemachinename", null };
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.MapNode(inputs, string.Empty));
        }

        [TestMethod]
        public void NodeEmpty_ReturnFalse()
        {
            var inputs = new string[] { "somemachinename", string.Empty };
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.MapNode(inputs, string.Empty));
        }
    }
}
