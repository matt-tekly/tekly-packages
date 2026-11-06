using System.Collections.Generic;
using Tekly.Stagecraft.PhysicsCore2D;

namespace Tekly.Stagecraft.Tests
{
	public class ContactRecorder : ActorBehaviour, IContactListener2D
	{
		public readonly List<ActorId> Begins = new List<ActorId>();
		public readonly List<ActorId> Ends = new List<ActorId>();
		public readonly List<ActorId> Selves = new List<ActorId>();
		public bool DespawnOtherOnBegin;

		public void OnContactBegin(in Contact2D contact)
		{
			Begins.Add(contact.Other);
			Selves.Add(contact.Self);

			if (DespawnOtherOnBegin && contact.OtherBody != null) {
				contact.OtherBody.Despawn();
			}
		}

		public void OnContactEnd(in Contact2D contact)
		{
			Ends.Add(contact.Other);
		}
	}
}
