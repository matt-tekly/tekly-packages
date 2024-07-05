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

			var message = new TopicFrame(bytes);

			Assert.That(message.Type, Is.EqualTo(TopicFrame.COMMAND_SUBSCRIBE));
			Assert.That(message.Headers, Contains.Key("Topic"));
			Assert.That(message.Headers["Topic"], Is.EqualTo("TEST_TOPIC"));
			Assert.That(message.Body, Is.Null);
		}

		[Test]
		public void SendFrame()
		{
			const string CONTENT = "SEND\r\nTopic:TEST_TOPIC\r\n\r\nBlingBong";

			var bytes = Encoding.UTF8.GetBytes(CONTENT);

			var message = new TopicFrame(bytes);
			
			Assert.That(message.Type, Is.EqualTo(TopicFrame.COMMAND_SEND));
			Assert.That(message.Headers, Contains.Key("Topic"));
			Assert.That(message.Headers["Topic"], Is.EqualTo("TEST_TOPIC"));
			Assert.That(message.Body, Is.EqualTo("BlingBong"));
		}
		
		[Test]
		public void EncodeFrame()
		{
			var frame = TopicFrame.EncodeFrame(TopicFrame.COMMAND_SEND, "TEST_TOPIC", "Blongus bingus");
			Debug.Log(frame);
		}
	}
}