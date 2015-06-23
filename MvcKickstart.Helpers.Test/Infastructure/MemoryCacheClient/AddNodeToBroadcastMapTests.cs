using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcKickstart.Infrastructure;

namespace MvcKickstartHelpers.Test.Infastructure
{
    [TestClass]
    public class AddNodeToBroadcastMapTests
    {
        [TestMethod]
        public void NullNodeName_NotAddedReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.AddNodeToBroadcastMap(null));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void EmptyNodeName_NotAddedReturnFalse()
        {
            var subject = new MemoryCacheClient();
            Assert.IsFalse(subject.AddNodeToBroadcastMap(string.Empty));
            Assert.AreEqual(0, subject.BroadcastNodes.Count);
        }

        [TestMethod]
        public void NonEmptyNullName_AddReturnTrue()
        {
            var subject = new MemoryCacheClient();
            const string text = "non empty null string";
            Assert.IsTrue(subject.AddNodeToBroadcastMap(text));
            Assert.AreEqual(1, subject.BroadcastNodes.Count);
            Assert.IsTrue(subject.BroadcastNodes.Contains(MemoryCacheClient.ConvertToUrl(text)));
        }
    }
}
