using MvcKickstart.Infrastructure;
using NUnit.Framework;


namespace MvcKickstartHelpers.Test.Infastructure
{
    public class ConvertToUrlTests: TestBase
    {
        [Test]
        public void NullNodeName_ReturnEmptyString()
        {
            Assert.AreEqual(string.Empty, MemoryCacheClient.ConvertToUrl(null));
        }

        [Test]
        public void EmptyNodeName_ReturnEmptyString()
        {
            Assert.AreEqual(string.Empty, MemoryCacheClient.ConvertToUrl(string.Empty));
        }

        [Test]
        public void NonEmptyNullName_ReturnStringWithHttp()
        {
            const string text = "non empty null string";
            const string expectedText = "http://" + text;
            Assert.AreEqual(expectedText, MemoryCacheClient.ConvertToUrl(text));
        }

        [Test]
        public void HttpName_ReturnHttpName()
        {
            const string text = "http://non empty null string";
            Assert.AreEqual(text, MemoryCacheClient.ConvertToUrl(text));
        }

        [Test]
        public void NonLowerHttpName_ReturnHttpName()
        {
            const string text = "HtTp://non empty null string";
            Assert.AreEqual(text, MemoryCacheClient.ConvertToUrl(text));
        }

    }
}
