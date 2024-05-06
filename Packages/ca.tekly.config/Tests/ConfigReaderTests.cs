using NUnit.Framework;

namespace Tekly.Config
{
  public class ConfigTests
  {
    [Test]
    public void TestDefaultConfig()
    {
      var config = new Tekly.Config.ConfigReader();

      Assert.IsFalse(config.Load());
      Assert.IsFalse(config.Get("key", false));
      Assert.AreEqual(0, config.Get("key", 0));
      Assert.AreEqual(0.0f, config.Get("key", 0.0f));
      Assert.AreEqual(0.0, config.Get("key", 0.0));
      Assert.AreEqual("", config.Get("key", ""));
    }
  }
}