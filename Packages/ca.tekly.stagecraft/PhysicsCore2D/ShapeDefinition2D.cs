using System;
using Unity.U2D.Physics;
using UnityEngine;

namespace Tekly.Stagecraft.PhysicsCore2D
{
	public enum ShapeKind2D
	{
		Circle,
		Capsule,
		Box
	}

	/// <summary>
	/// A shape to create on an <see cref="ActorBody2D"/>'s body each time it spawns. Anything beyond this (contact
	/// filters, materials) is set on the created <see cref="PhysicsShape"/> from game code.
	/// </summary>
	[Serializable]
	public sealed class ShapeDefinition2D
	{
		public ShapeKind2D Kind = ShapeKind2D.Circle;
		public Vector2 Center;

		[Tooltip("Circle and capsule radius")]
		public float Radius = 0.5f;

		[Tooltip("Capsule: second centre, relative to Center")]
		public Vector2 CapsuleOffset = new Vector2(0f, 1f);

		[Tooltip("Box: full size")]
		public Vector2 Size = Vector2.one;

		public bool IsTrigger;

		[Tooltip("Produce contact begin/end events (either shape of a pair needs this)")]
		public bool ContactEvents;

		[Tooltip("Produce trigger begin/end events (both shapes of a pair need this)")]
		public bool TriggerEvents;

		public float Density = 1f;

		public PhysicsShape Create(PhysicsBody body)
		{
			var definition = PhysicsShapeDefinition.defaultDefinition;
			definition.isTrigger = IsTrigger;
			definition.contactEvents = ContactEvents;
			definition.triggerEvents = TriggerEvents;
			definition.density = Density;

			switch (Kind) {
				case ShapeKind2D.Capsule:
					return body.CreateShape(CapsuleGeometry.Create(Center, Center + CapsuleOffset, Radius), definition);
				case ShapeKind2D.Box:
					return body.CreateShape(PolygonGeometry.CreateBox(Size, 0f, new PhysicsTransform(Center), false), definition);
				default:
					return body.CreateShape(CircleGeometry.Create(Radius, Center), definition);
			}
		}
	}
}
