using System.Collections;
using System.Collections.Generic;
using System;
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
  
  [TestFixture]
  public class ConfigReaderGetIntTests
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
    public void Get_IntWithValidValue_ReturnsValue()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"intValue", "42"},
      });

      Assert.AreEqual(42, config.Get("intValue", 0));
    }

    [Test]
    public void Get_IntWithInvalidValue_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"boolValue", "true"},
        {"boolValueFalse", "false"},
        {"floatValue", "3.14"},
        {"doubleValue", "3.14"},
        {"stringValue", "Hello"}
      });

      Assert.AreEqual(0, config.Get("boolValue", 0));
      Assert.AreEqual(0, config.Get("boolValueFalse", 0));
      Assert.AreEqual(0, config.Get("floatValue", 0));
      Assert.AreEqual(0, config.Get("doubleValue", 0));
      Assert.AreEqual(0, config.Get("stringValue", 0));
    }

    [Test]
    public void Get_IntWithMissingKey_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"intValue", "42"},
      });

      Assert.AreEqual(0, config.Get("intValueMissing", 0));
    }
  }

  [TestFixture]
  public class ConfigReaderGetFloatTests
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

    [Test]
    public void Get_FloatWithValidValue_ReturnsValue()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"floatValue", "3.14"},
        {"doubleValue", "3.14"},
        {"intValue", "42"}
      });

      Assert.IsTrue(Math.Abs(3.14f - config.Get("floatValue", 0.0f)) < 1e-5);
      Assert.IsTrue(Math.Abs(3.14f - config.Get("doubleValue", 0.0f)) < 1e-5);
      Assert.IsTrue(Math.Abs(42f - config.Get("intValue", 0.0f)) < 1e-5);
    }

    [Test]
    public void Get_FloatWithInvalidValue_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"boolValue", "true"},
        {"boolValueFalse", "false"},
        {"stringValue", "Hello"}
      });

      Assert.AreEqual(0.0f, config.Get("boolValue", 0.0f));
      Assert.AreEqual(0.0f, config.Get("boolValueFalse", 0.0f));

      Assert.AreEqual(0.0f, config.Get("stringValue", 0.0f));
    }

    [Test]
    public void Get_FloatWithMissingKey_ReturnsDefault()
    {
      var config = new Tekly.Config.ConfigReader();
      config.Load(new Dictionary<string, string>
      {
        {"floatValue", "3.14"},
      });

      Assert.AreEqual(0.0f, config.Get("floatValueMissing", 0.0f));
    }
  }
}