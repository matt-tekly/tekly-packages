using System;
using System.Collections.Generic;
using System.Reflection;
using Tekly.DevBoard.Components;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Base for everything in a DevBoard. Behaviours that override Tick are ticked by the Board they live in,
	/// at that board's tick rate, and only while enabled, so hidden pages cost nothing. Behaviours outside a
	/// Board are ticked every frame by DevBoard.
	///
	/// A Tick that throws is logged once and the behaviour is marked faulted. Widget getters often reach into
	/// game objects that can be destroyed or unloaded, so a faulted behaviour keeps retrying at a slower rate
	/// until it succeeds again instead of spamming the console.
	/// </summary>
	public class DevBoardBehaviour : MonoBehaviour
	{
		/// <summary>
		/// True while Tick is throwing.
		/// </summary>
		public bool IsFaulted => m_faulted;

		/// <summary>
		/// This behaviour's slot in its TickGroup, or -1 when it isn't in one.
		/// </summary>
		internal int TickIndex { get; set; } = -1;

		private const float FAULTED_RETRY_INTERVAL = 0.5f;

		private static readonly Dictionary<Type, bool> s_overridesTick = new();

		private TickGroup m_tickGroup;
		private bool m_faulted;
		private float m_nextRetryTime;

		protected virtual void OnEnable()
		{
			RegisterTick();
		}

		protected virtual void OnDisable()
		{
			UnregisterTick();
		}

		protected virtual void OnTransformParentChanged()
		{
			// Moved to another container, which may belong to a different Board
			if (isActiveAndEnabled) {
				UnregisterTick();
				RegisterTick();
			}
		}

		protected virtual void Tick()
		{

		}

		/// <summary>
		/// Called once when Tick starts throwing. Use it to show an error state.
		/// </summary>
		protected virtual void OnFaulted(Exception exception)
		{

		}

		/// <summary>
		/// Called once when Tick succeeds again after being faulted.
		/// </summary>
		protected virtual void OnRecovered()
		{

		}

		/// <summary>
		/// Called by the TickGroup. Never throws.
		/// </summary>
		internal void RunTick()
		{
			if (m_faulted && Time.realtimeSinceStartup < m_nextRetryTime) {
				return;
			}

			try {
				Tick();
			} catch (Exception exception) {
				m_nextRetryTime = Time.realtimeSinceStartup + FAULTED_RETRY_INTERVAL;

				if (!m_faulted) {
					m_faulted = true;
					Debug.LogException(exception, this);
					OnFaulted(exception);
				}

				return;
			}

			if (m_faulted) {
				m_faulted = false;
				OnRecovered();
			}
		}

		private void RegisterTick()
		{
			// Most widgets (labels, buttons, containers) never tick, so they stay out of the group entirely
			if (!OverridesTick(GetType())) {
				return;
			}

			m_tickGroup = FindTickGroup();
			m_tickGroup?.Add(this);
		}

		private void UnregisterTick()
		{
			m_tickGroup?.Remove(this);
			m_tickGroup = null;
		}

		private TickGroup FindTickGroup()
		{
			// Start from the parent so a Board never registers with itself
			var parent = transform.parent;
			var board = parent != null ? parent.GetComponentInParent<Board>() : null;

			if (board != null) {
				return board.TickGroup;
			}

			// Null outside of play mode
			return DevBoard.Instance?.UnboardedTickGroup;
		}

		private static bool OverridesTick(Type type)
		{
			if (!s_overridesTick.TryGetValue(type, out var overrides)) {
				var tick = type.GetMethod(nameof(Tick), BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				overrides = tick != null && tick.DeclaringType != typeof(DevBoardBehaviour);
				s_overridesTick[type] = overrides;
			}

			return overrides;
		}
	}
}
