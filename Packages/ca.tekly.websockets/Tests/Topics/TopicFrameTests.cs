using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Tekly.WebSockets.Topics
{
	public class TopicFrameTests
	{
		[Test]
		public void Subscribe()
		{
			const string CONTENT = "SUBSCRIBE\r\nTopic:TEST_TOPIC\r\n\r\n";

			var bytes = Encoding.UTF8.GetBytes(CONTENT);

			var frame = new TopicFrame(bytes);

			Assert.That(frame.Type, Is.EqualTo(FrameCommands.SUBSCRIBE));
			Assert.That(frame.Headers, Contains.Key("Topic"));
			Assert.That(frame.Headers["Topic"], Is.EqualTo("TEST_TOPIC"));
			Assert.That(frame.Content, Is.Null);
		}

		[Test]
		public void SendFrame()
		{
			const string CONTENT = "SEND\r\nTopic:TEST_TOPIC\r\n\r\nBlingBong";

			var bytes = Encoding.UTF8.GetBytes(CONTENT);

			var frame = new TopicFrame(bytes);
			
			Assert.That(frame.Type, Is.EqualTo(FrameCommands.SEND));
			Assert.That(frame.Headers, Contains.Key("Topic"));
			Assert.That(frame.Headers["Topic"], Is.EqualTo("TEST_TOPIC"));
			Assert.That(frame.Content, Is.EqualTo("BlingBong"));
		}
		
		[Test]
		public void EncodeDecodeFrame()
		{
			var frameText = TopicFrame.EncodeFrame(FrameCommands.SEND, "TEST_TOPIC", "Text", "Hello there");
			
			TestContext.Write(frameText);
			var bytes = Encoding.UTF8.GetBytes(frameText);

			var frame = new TopicFrame(bytes);
			
			Assert.That(frame.Type, Is.EqualTo(FrameCommands.SEND));
			Assert.That(frame.Headers, Contains.Key("Topic"));
			Assert.That(frame.Headers["Topic"], Is.EqualTo("TEST_TOPIC"));
			
			Assert.That(frame.Headers, Contains.Key("Content-Type"));
			Assert.That(frame.Headers["Content-Type"], Is.EqualTo("Text"));
			
			Assert.That(frame.Content, Is.EqualTo("Hello there"));
		}
	}
}