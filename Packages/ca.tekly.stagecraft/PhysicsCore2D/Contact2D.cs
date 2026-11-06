using Unity.U2D.Physics;

namespace Tekly.Stagecraft.PhysicsCore2D
{
	/// <summary>
	/// One side of a recorded contact or trigger pair, delivered at the stage's event phase. <see cref="Other"/>
	/// is the other actor's id at the time of the event; <see cref="OtherBody"/> is null if that actor is no
	/// longer alive or the other shape belongs to no actor. Shape handles may be invalid by delivery time.
	/// </summary>
	public readonly struct Contact2D
	{
		public readonly ActorId Self;
		public readonly ActorId Other;
		public readonly ActorBody2D OtherBody;
		public readonly PhysicsShape Shape;
		public readonly PhysicsShape OtherShape;

		public Contact2D(ActorId self, ActorId other, ActorBody2D otherBody, PhysicsShape shape, PhysicsShape otherShape)
		{
			Self = self;
			Other = other;
			OtherBody = otherBody;
			Shape = shape;
			OtherShape = otherShape;
		}
	}

	/// <summary>Implemented by a behaviour on an actor with an <see cref="ActorBody2D"/> to receive contacts.</summary>
	public interface IContactListener2D
	{
		void OnContactBegin(in Contact2D contact);
		void OnContactEnd(in Contact2D contact);
	}

	/// <summary>Implemented by a behaviour on an actor with an <see cref="ActorBody2D"/> to receive trigger overlaps, as the trigger or as the visitor.</summary>
	public interface ITriggerListener2D
	{
		void OnTriggerBegin(in Contact2D contact);
		void OnTriggerEnd(in Contact2D contact);
	}
}
