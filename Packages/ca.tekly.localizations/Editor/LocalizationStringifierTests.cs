using NUnit.Framework;

namespace Tekly.Localizations
{
    [TestFixture]
    public class LocalizationStringifierTests
    {
        [Test]
        public void SimpleTest()
        {
            const string format = "Count";

            LocalizationStringifier.Stringify(format, out var outFormat, out var outKeys);
            
            Assert.That(outFormat, Is.EqualTo(format));
            Assert.That(outKeys, Is.Null);
        }
        
        [Test]
        public void ParseWithFormat()
        {
            string format = "Count: {count:0N}";

            LocalizationStringifier.Stringify(format, out var outFormat, out var outKeys);

            Assert.That(outFormat, Is.EqualTo("Count: {0:0N}"));
            Assert.That(outKeys.Length, Is.EqualTo(1));
            Assert.That(outKeys[0], Is.EqualTo("count"));
        }
        
        [Test]
        public void ParseWithFormat2()
        {
            string format = "{count:1N} Count: {count:0N}";

            LocalizationStringifier.Stringify(format, out var outFormat, out var outKeys);

            Assert.That(outFormat, Is.EqualTo("{0:1N} Count: {0:0N}"));
            Assert.That(outKeys.Length, Is.EqualTo(1));
            Assert.That(outKeys[0], Is.EqualTo("count"));
        }
        
        [Test]
        public void StringWithMultipleKeys()
        {
            string format = "{key1:0N} Count: {key2:0N} {key1} {key3}";

            LocalizationStringifier.Stringify(format, out var outFormat, out var outKeys);

            Assert.That(outFormat, Is.EqualTo("{0:0N} Count: {1:0N} {0} {2}"));
            Assert.That(outKeys.Length, Is.EqualTo(3));
            Assert.That(outKeys[0], Is.EqualTo("key1"));
            Assert.That(outKeys[1], Is.EqualTo("key2"));
            Assert.That(outKeys[2], Is.EqualTo("key3"));
        }
    }
}