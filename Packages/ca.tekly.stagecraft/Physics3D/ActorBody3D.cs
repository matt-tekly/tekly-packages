using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Stagecraft.Physics3D
{
	/// <summary>
	/// The 3D counterpart of ActorBody2D. PhysX has no user data, so the actor id lives here: a contact finds the
	/// other actor through the other collider's ActorBody3D and its id at the time of the event. The Rigidbody stays
	/// on the pooled object (PhysX bodies are components, not handles); the inactive GameObject keeps it out of the world.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public sealed class ActorBody3D : ActorBehaviour
	{
		public Rigidbody Rigidbody => m_rigidbody;

		private Rigidbody m_rigidbody;
		private Physics3DAdapter m_adapter;
		private IContactListener3D[] m_contactListeners;
		private ITriggerListener3D[] m_triggerListeners;

		public override void OnCreated()
		{
			m_adapter = Stage.Physics as Physics3DAdapter;
			m_rigidbody = GetComponent<Rigidbody>();
			CollectListeners();
		}

		public override void OnSpawned()
		{
			m_adapter?.Register(this);
		}

		public override void OnDespawned()
		{
			m_adapter?.Unregister(this);
		}

		private void OnCollisionEnter(Collision collision)
		{
			Record(collision.collider, false, true);
		}

		private void OnCollisionExit(Collision collision)
		{
			Record(collision.collider, false, false);
		}

		private void OnTriggerEnter(Collider other)
		{
			Record(other, true, true);
		}

		private void OnTriggerExit(Collider other)
		{
			Record(other, true, false);
		}

		// Physics messages only record; the adapter delivers them at the stage's event phase
		private void Record(Collider other, bool trigger, bool begin)
		{
			if (ReferenceEquals(m_adapter, null) || !IsAlive) {
				return;
			}

			var otherId = ActorId.None;
			if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent<ActorBody3D>(out var otherBody) && otherBody.IsAlive) {
				otherId = otherBody.Id;
			}

			m_adapter.Record(Id, otherId, trigger, begin);
		}

		internal void Deliver(bool trigger, bool begin, in Contact3D contact)
		{
			if (trigger) {
				for (var i = 0; i < m_triggerListeners.Length; i++) {
					if (begin) {
						m_triggerListeners[i].OnTriggerBegin(contact);
					} else {
						m_triggerListeners[i].OnTriggerEnd(contact);
					}
				}
			} else {
				for (var i = 0; i < m_contactListeners.Length; i++) {
					if (begin) {
						m_contactListeners[i].OnContactBegin(contact);
					} else {
						m_contactListeners[i].OnContactEnd(contact);
					}
				}
			}
		}

		private void CollectListeners()
		{
			var behaviours = Actor.Behaviours;

			using (ListPool<IContactListener3D>.Get(out var contacts))
			using (ListPool<ITriggerListener3D>.Get(out var triggers)) {
				for (var i = 0; i < behaviours.Length; i++) {
					if (behaviours[i] is IContactListener3D contact) {
						contacts.Add(contact);
					}

					if (behaviours[i] is ITriggerListener3D trigger) {
						triggers.Add(trigger);
					}
				}

				m_contactListeners = contacts.ToArray();
				m_triggerListeners = triggers.ToArray();
			}
		}
	}
}
