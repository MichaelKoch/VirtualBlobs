using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualBlobs.Tests;

[TestClass]
public class ProviderTestClass
{
    [TestMethod]
    public void Can_Create_Provider()
    {
        Assert.AreEqual("1", "1");

    }
}