using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Config
{
  [TestFixture]
  public class ConfigReaderLoadTests
  {
    private IDictionary<string, string> m_defaultConfig = new Dictionary<string, string>
    {
      {"boolValue", "true"},
      {"boolValueFalse", "false"},
      {"intValue", "42"},
      {"floatValue", "3.14"},
      {"doubleValue", "3.14"},
      {"stringValue", "Hello"}
    };

    [SetUp]
    public void Setup()
    {
      // noop
    }

    [Test]
    public void TestLoadNoConfig()
    {
      var config = new Tekly.Config.ConfigReader();

      Assert.IsFalse(config.Load(null));
    }

    [Test]
    public void TestLoadEmptyConfig()
    {
      var config = new Tekly.Config.ConfigReader();

      Assert.IsTrue(config.Load(new Dictionary<string, string>()));
    }

    [Test]
    public void TestLoadConfig()
    {
      var config = new Tekly.Config.ConfigReader();

      Assert.IsTrue(config.Load(m_defaultConfig));
    }
  }

  [TestFixture]
  public class ConfigReaderGetBoolTests
  {
    private IDictionary<string, string> m_defaultConfig = new Dictionary<string, string>
    {
      {"boolValue", "true"},
      {"boolValueFalse", "false"},
      {"intBoolValue", "1"},
      {"floatBoolValue", "1.0f"},
      {"doubleBoolValue", "1.0"},
      {"stringBoolValue", "True"},
      {"intValue", "42"},
      {"floatValue", "3.14"},
      {"doubleValue", "3.14"},
      {"stringValue", "Hello"}
    };

    [SetUp]
    public void Setup()
    {
      // noop
    }

    [Test]
    public void Get_BoolWithValidValue_ReturnsValue()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"boolValue", "true"},
        {"boolValueFalse", "false"},
        {"stringBoolTrueValue", "True"},
        {"stringBoolFalseValue", "False"}
      });

      Assert.IsTrue(config.Get("boolValue", false));
      Assert.IsTrue(config.Get("stringBoolTrueValue", false));
      Assert.IsFalse(config.Get("boolValueFalse", true));
      Assert.IsFalse(config.Get("stringBoolFalseValue", true));
    }

    [Test]
    public void Get_BoolWithInvalidValue_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"intValue", "42"},
        {"floatValue", "3.14"},
        {"doubleValue", "3.14"},
        {"stringValue", "Hello"},
        {"intBoolValue", "1"},
        {"floatBoolValue", "1.0f"},
        {"doubleBoolValue", "1.0"},
      });

      Assert.IsFalse(config.Get("intValue", false));
      Assert.IsFalse(config.Get("floatValue", false));
      Assert.IsFalse(config.Get("doubleValue", false));
      Assert.IsFalse(config.Get("stringValue", false));
      Assert.IsFalse(config.Get("intBoolValue", false));
      Assert.IsFalse(config.Get("floatBoolValue", false));
      Assert.IsFalse(config.Get("doubleBoolValue", false));
    }

    [Test]
    public void Get_BoolWithMissingKey_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"boolValue", "true"},
      });

      Assert.IsTrue(config.Get("boolValueMissing", true));
    }
  }
}