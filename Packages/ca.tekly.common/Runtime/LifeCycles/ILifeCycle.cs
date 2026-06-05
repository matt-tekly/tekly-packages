using System.Collections;
using UnityEngine;

namespace Tekly.Common.LifeCycles
{
	public delegate void UpdateDelegate();
	public delegate void QuitDelegate();
	public delegate void FocusDelegate(bool hasFocus);
	public delegate void PauseDelegate(bool paused);

	public interface ILifeCycle
	{
		event UpdateDelegate Update;
		event QuitDelegate Quit;
		event FocusDelegate Focus;
		event PauseDelegate Pause;

		bool IsQuitting { get; }
		bool IsFocused { get; }
		bool IsPaused { get; }

		Coroutine StartCoroutine(IEnumerator enumerator);
	}
}