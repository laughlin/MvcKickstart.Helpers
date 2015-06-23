using Microsoft.VisualStudio.TestTools.UnitTesting;
using MvcKickstart.Infrastructure;

namespace MvcKickstartHelpers.Test.Infastructure
{
    [TestClass]
    public class ConvertToUrlTests
    {
        [TestMethod]
        public void NullNodeName_ReturnEmptyString()
        {
            Assert.AreEqual(string.Empty, MemoryCacheClient.ConvertToUrl(null));
        }

        [TestMethod]
        public void EmptyNodeName_ReturnEmptyString()
        {
            Assert.AreEqual(string.Empty, MemoryCacheClient.ConvertToUrl(string.Empty));
        }

        [TestMethod]
        public void NonEmptyNullName_ReturnStringWithHttp()
        {
            const string text = "non empty null string";
            const string expectedText = "http://" + text;
            Assert.AreEqual(expectedText, MemoryCacheClient.ConvertToUrl(text));
        }

        [TestMethod]
        public void HttpName_ReturnHttpName()
        {
            const string text = "http://non empty null string";
            Assert.AreEqual(text, MemoryCacheClient.ConvertToUrl(text));
        }

        [TestMethod]
        public void NonLowerHttpName_ReturnHttpName()
        {
            const string text = "HtTp://non empty null string";
            Assert.AreEqual(text, MemoryCacheClient.ConvertToUrl(text));
        }

    }
}
