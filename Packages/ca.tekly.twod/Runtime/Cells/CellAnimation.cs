using System;
using System.Linq;
using Tekly.EditorUtils.Attributes;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[Serializable]
	public class CellFrameEvt { }

	[Serializable]
	public class TextEvt : CellFrameEvt
	{
		public string Text;
	}
	
	[Serializable]
	public class AudioEvt : CellFrameEvt
	{
		public string Clip;
	}

	[Serializable]
	public class StopEvt : CellFrameEvt { }
    
	[Serializable]
	public struct SpriteFrame
	{
		public float Duration;
		public Sprite Sprite;
		[SerializeReference, Polymorphic] public CellFrameEvt Event;
	}
	
	public class CellAnimation : ScriptableObject
	{
		[NonSerialized] private string m_name;
		public string Name => m_name ??= name;
		
		public SpriteFrame[] Frames;
		public float Duration => Frames.Sum(x => x.Duration);
	}
}