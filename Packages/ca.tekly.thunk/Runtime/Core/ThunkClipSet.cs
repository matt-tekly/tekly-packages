using System;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// A set of interchangeable AudioClips and how one is picked each time the set plays.
	/// </summary>
	[Serializable]
	public struct ThunkClipSet
	{
		public AudioClip[] Clips;
		public RandomSelectMode Mode;

		public int Count => Clips != null ? Clips.Length : 0;
	}
}
