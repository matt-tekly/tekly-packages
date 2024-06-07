using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using NUnit.Framework;

namespace Tekly.Config
{
  [TestFixture]
  public class ConfigParserTests
  {

    public string m_validConfig = ""+
    "boolValue#editor#true\n"+
    "intValue#android#42\n"+
    "floatValue#android#3.14\n"+
    "doubleValue#editor#3.14\n"+
    "stringValue#editor#Hello";

    [Test]
    public void TestParse_NoConfig()
    {
      var config = new Tekly.Config.ConfigParser(RuntimePlatform.OSXEditor);

      Assert.IsFalse(config.Parse(null));
      Assert.IsFalse(config.Parse(""));
    }


    [Test]
    public void TestParse_InvalidPlatform()
    {
      Assert.Throws<InvalidOperationException>(() => new Tekly.Config.ConfigParser(RuntimePlatform.Stadia));
    }

    
    [Test]
    public void TestParse_VaildConfig()
    {
      var config = new Tekly.Config.ConfigParser(RuntimePlatform.OSXEditor);

      Assert.IsTrue(config.Parse(m_validConfig));
    }


    [Test]
    public void TestParse_VaildConfig_CorrectValuesForPlatform()
    {
      var config = new Tekly.Config.ConfigParser(RuntimePlatform.OSXEditor);
      config.Parse(m_validConfig);
      
      var dictionary = config.ToDictionary();

      Assert.AreEqual("true", dictionary["boolValue"]);
      Assert.AreEqual("3.14", dictionary["doubleValue"]);
      Assert.AreEqual("Hello", dictionary["stringValue"]);
      Assert.IsFalse(dictionary.ContainsKey("intValue"));
      Assert.IsFalse(dictionary.ContainsKey("floatValue"));
    }
  }
}