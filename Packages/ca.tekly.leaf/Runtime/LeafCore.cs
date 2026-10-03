using System;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Leaf.Elements;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf
{
	public class LeafCore : Singleton<LeafCore>, IDisposable
	{
		public readonly Latch DisableInput = new();
		public readonly LeafSelection Selection = new();

		public LeafCore()
		{
			LifeCycle.Instance.LateUpdate += OnLateUpdate;
		}

		public IDisposable DisableInputScope(object owner)
		{
			return DisableInput.HoldScope(owner);
		}

		/// <summary>
		/// Clicks target through the EventSystem as a left click would, e.g. for a tutorial step. Works for any
		/// element that handles clicks, Unity's own included. Returns false when there's no EventSystem, or target
		/// is inactive or doesn't handle clicks. A non-interactable element still returns true: its handler runs
		/// and does nothing.
		/// </summary>
		public bool Click(GameObject target)
		{
			var eventSystem = EventSystem.current;
			if (target == null || eventSystem == null) {
				return false;
			}

			var eventData = new PointerEventData(eventSystem) {
				button = PointerEventData.InputButton.Left
			};

			return ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
		}

		public void Dispose()
		{
			LifeCycle.Instance.LateUpdate -= OnLateUpdate;
		}

		private void OnLateUpdate()
		{
			// Tab first, so subscribers hear about the selection it moved to in the same frame
			LeafNavigationScope.ProcessTab();
			Selection.Refresh();
		}
	}
}
