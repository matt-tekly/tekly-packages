using System;
using System.Collections.Generic;
using Tekly.Logging;
using Unity.U2D.Physics;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Stagecraft.PhysicsCore2D
{
	/// <summary>
	/// Gives an actor a low-level 2D body for as long as it is alive: created in OnSpawned, destroyed in
	/// OnDespawned (the despawn sync point). The body's user data is the actor's id, so physics events resolve
	/// through the stage and a despawned or reused actor resolves to nothing. The stage must have a
	/// <see cref="PhysicsCore2DAdapter"/> attached.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class ActorBody2D : ActorBehaviour
	{
		public PhysicsBody Body => m_body;
		public bool HasBody => m_body.isValid;

		public PhysicsBody.BodyType BodyType {
			get => m_bodyType;
			set => m_bodyType = value;
		}

		public bool FixedRotation {
			get => m_fixedRotation;
			set => m_fixedRotation = value;
		}

		public float GravityScale {
			get => m_gravityScale;
			set => m_gravityScale = value;
		}

		public float LinearDamping {
			get => m_linearDamping;
			set => m_linearDamping = value;
		}

		public PhysicsBody.TransformWriteMode TransformWriteMode {
			get => m_transformWriteMode;
			set => m_transformWriteMode = value;
		}

		public ShapeDefinition2D[] Shapes {
			get => m_shapes;
			set => m_shapes = value;
		}

		/// <summary>The shapes created from <see cref="Shapes"/>, in the same order. Valid while the actor is alive.</summary>
		public ReadOnlySpan<PhysicsShape> CreatedShapes => m_createdShapes;

		[SerializeField] private PhysicsBody.BodyType m_bodyType = PhysicsBody.BodyType.Dynamic;
		[SerializeField] private bool m_fixedRotation = true;
		[SerializeField] private float m_gravityScale;
		[SerializeField] private float m_linearDamping;
		[SerializeField] private PhysicsBody.TransformWriteMode m_transformWriteMode = PhysicsBody.TransformWriteMode.Current;
		[SerializeField] private ShapeDefinition2D[] m_shapes = { new ShapeDefinition2D() };

		private PhysicsBody m_body;
		private PhysicsShape[] m_createdShapes = Array.Empty<PhysicsShape>();
		private int m_ownerKey;
		private PhysicsCore2DAdapter m_adapter;
		private IContactListener2D[] m_contactListeners;
		private ITriggerListener2D[] m_triggerListeners;

		private static readonly TkLogger s_logger = TkLogger.Get<ActorBody2D>();
		private static bool s_loggedNoAdapter;

		public override void OnCreated()
		{
			m_adapter = Stage.Physics as PhysicsCore2DAdapter;
			if (m_adapter == null) {
				if (!s_loggedNoAdapter) {
					s_loggedNoAdapter = true;
					s_logger.Error("ActorBody2D on [{actor}] but the stage has no PhysicsCore2DAdapter; attach one before spawning. (Logged once)", ("actor", Actor.name));
				}

				return;
			}

			CollectListeners();
		}

		public override void OnSpawned()
		{
			if (ReferenceEquals(m_adapter, null)) {
				return;
			}

			var definition = PhysicsBodyDefinition.defaultDefinition;
			definition.type = m_bodyType;
			definition.position = transform.position;
			definition.rotation = PhysicsRotate.FromDegrees(transform.eulerAngles.z);
			definition.gravityScale = m_gravityScale;
			definition.linearDamping = m_linearDamping;
			definition.constraints = m_fixedRotation ? PhysicsBody.BodyConstraints.Rotation : PhysicsBody.BodyConstraints.None;
			definition.transformWriteMode = m_transformWriteMode;

			m_body = m_adapter.World.CreateBody(definition);
			m_ownerKey = m_body.SetOwner(this);
			m_body.userData = new PhysicsUserData { int64Value = Id.Pack() };
			m_body.transformObject = transform;

			if (m_createdShapes.Length != m_shapes.Length) {
				m_createdShapes = new PhysicsShape[m_shapes.Length];
			}

			for (var i = 0; i < m_shapes.Length; i++) {
				m_createdShapes[i] = m_shapes[i].Create(m_body);
			}

			m_adapter.Register(this);
		}

		public override void OnDespawned()
		{
			if (ReferenceEquals(m_adapter, null)) {
				return;
			}

			m_adapter.Unregister(this);

			if (m_body.isValid) {
				m_body.Destroy(m_ownerKey);
			}

			m_body = default;
			Array.Clear(m_createdShapes, 0, m_createdShapes.Length);
		}

		internal void Deliver(bool isContact, bool isBegin, in Contact2D contact)
		{
			if (isContact) {
				for (var i = 0; i < m_contactListeners.Length; i++) {
					if (isBegin) {
						m_contactListeners[i].OnContactBegin(contact);
					} else {
						m_contactListeners[i].OnContactEnd(contact);
					}
				}
			} else {
				for (var i = 0; i < m_triggerListeners.Length; i++) {
					if (isBegin) {
						m_triggerListeners[i].OnTriggerBegin(contact);
					} else {
						m_triggerListeners[i].OnTriggerEnd(contact);
					}
				}
			}
		}

		private void CollectListeners()
		{
			var behaviours = Actor.Behaviours;

			using (ListPool<IContactListener2D>.Get(out var contacts))
			using (ListPool<ITriggerListener2D>.Get(out var triggers)) {
				for (var i = 0; i < behaviours.Length; i++) {
					if (behaviours[i] is IContactListener2D contact) {
						contacts.Add(contact);
					}

					if (behaviours[i] is ITriggerListener2D trigger) {
						triggers.Add(trigger);
					}
				}

				m_contactListeners = contacts.ToArray();
				m_triggerListeners = triggers.ToArray();
			}
		}
	}
}
