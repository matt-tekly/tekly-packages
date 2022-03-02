using NUnit.Framework;

namespace Tekly.Common.Expressions
{
    public class ExpressionTest
    {
        [Test]
        public void Test()
        {
            ExpressionFactory factory = new ExpressionFactory();
            var container = factory.Create("[a] < [b]");
            
            Assert.That(container.Parameters.Length, Is.EqualTo(2));
            Assert.That(container.Parameters[0], Is.EqualTo("a"));
            Assert.That(container.Parameters[1], Is.EqualTo("b"));
            Assert.That(container.Parameters.Length, Is.EqualTo(2));
        }
    }
}