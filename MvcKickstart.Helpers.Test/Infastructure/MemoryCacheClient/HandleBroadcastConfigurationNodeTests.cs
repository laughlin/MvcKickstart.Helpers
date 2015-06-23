using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcKickstart.Infrastructure;

namespace MvcKickstartHelpers.Test.Infastructure
{
    [TestClass]
    public class HandleBroadcastConfigurationNodeTests
    {
        [TestMethod]
        public void NullConfig_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(null));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void EmptyConfig_ReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(string.Empty));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        #region Non Map
        [TestMethod]
        public void NonMapMachineName_NoAddReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(Environment.MachineName));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void NonMapNonMachineName_AddReturnTrue()
        {
            const string text = "somemachinename";
            var httpText = MemoryCacheClient.ConvertToUrl(text);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpText));
        }
        #endregion

        #region MAP
        [TestMethod]
        public void MapMachineName_NoAddReturnFalse()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}", Environment.MachineName, url);
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void MapNonMachineName_AddReturnTrue()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}", "somemachinename", url);
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void MapMachineNameForce_AddReturnTrue()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", Environment.MachineName, url, "-a-b-c-force-d-e-f-g");
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void MapMachineNameIgnoreLocal_AddReturnTrue()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", Environment.MachineName, url, "-x-y-ignorelocal-g-h-i-j");
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void MapMachineNameForceIgnoreLocal_AddReturnTrue()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", Environment.MachineName, url, "-a-b-c-force-d-f-g-h-ignorelocal-i-j-k-l-m-");
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void MapMachineNameNonSupportedConfig_NoAddReturnFalse()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", Environment.MachineName, url, "-a-b-c-d-f-g-h--i-j-k-l-m-");
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void MapNonMachineNameWithConfig_AddReturnTrue()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", "somemachinename", url, "-a-b-c-d-f-g-h--i-j-k-l-m-");
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void Map4Sections_NoAddReturnFalse()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}:xyz", Environment.MachineName, url, "-a-b-c-d-f-g-h--i-j-k-l-m-");
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void MapEmptyFirstSection_AddReturnTrue()
        {
            const string url = "someurl";
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var text = string.Format("{0}:{1}", string.Empty, url);
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }

        [TestMethod]
        public void MapEmptySecondSection_NoAddReturnFalse()
        {
            const string url = "";
            var text = string.Format("{0}:{1}", "someenvironment", url);
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void MapEmptyThirdSectionMachineName_NoAddReturnFalse()
        {
            const string url = "someurl";
            var text = string.Format("{0}:{1}:{2}", Environment.MachineName, url,"");
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void MapEmptyThirdSectionNonMachineName_AddReturnTrue()
        {
            const string url = "someurl";
            var httpUrl = MemoryCacheClient.ConvertToUrl(url);
            var text = string.Format("{0}:{1}:{2}", "someenvironment", url, "");
            var subject = new MemoryCacheClient();
            Assert.IsTrue(subject.HandleBroadcastConfigurationNode(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(httpUrl));
        }


        
        #endregion


    }
}
