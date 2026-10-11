using System;
using System.Collections.Generic;
using Tekly.DevBoard.Commands;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Console
{
	/// <summary>
	/// A terminal for developers, docked along the bottom of the screen: output scrolls above a line to type
	/// commands into (see DevCommandAttribute and DevBoard.Command). It's not modal: the game and the panels keep
	/// working around it, and it shows over the game with DevBoard hidden.
	///
	/// While typing, suggestions open above the line: command names until one is complete, then values for the
	/// argument at the caret, from the parameter's suggestions. A hint shows the command's arguments, or what's
	/// wrong with the line so far.
	///
	/// Keys: Tab completes with the highlighted suggestion, or the first. Up and Down (and Shift+Tab) move the
	/// highlight, or go through history when there are no suggestions. Enter takes the highlighted suggestion if
	/// there is one, otherwise runs the line. Escape closes the suggestions, then the console. Output is kept
	/// between openings, history between sessions too.
	/// </summary>
	internal sealed class DevConsole
	{
		private const float HEIGHT_FRACTION = 0.4f;
		private const int MAX_OUTPUT = 200;
		private const int MAX_HISTORY = 100;
		private const int SCROLL_TO_END_FRAMES = 2;

		// Space between the suggestion box and the line or hint it stands on
		private const float SUGGESTION_GAP = 8f;
		private const string HISTORY_KEY = "Tekly.DevBoard.ConsoleHistory";

		private const string ECHO = "<alpha=#80>> <noparse>{0}</noparse><alpha=#FF>";
		private const string ERROR = "<color=#E5534B>{0}</color>";
		private const string DIM = "<alpha=#80>{0}<alpha=#FF>";
		private const string DIMMER = "<alpha=#55>{0}<alpha=#FF>";
		private const string PICKED = "<color=#F6C050>{0}</color>";

		public bool IsOpen => m_root != null;

		private readonly DevBoardUI m_board;
		private readonly CommandRegistry m_commands;

		// Rich text, oldest first, kept while the console is closed
		private readonly List<string> m_output = new();
		private readonly List<string> m_history;

		private readonly List<LabelWidget> m_suggestionLabels = new();
		private static readonly Vector3[] s_corners = new Vector3[4];

		private ConsoleWidget m_root;
		private ScrollViewWidget m_scroll;
		private ContainerWidget m_suggestionBox;
		private ConsoleOutput m_outputField;
		private LabelWidget m_hint;
		private TextInputWidget m_input;

		private string m_line = string.Empty;
		private int m_caret;
		private CommandCompletion m_completion = CommandCompletion.Empty;

		// -1 while nothing's picked: Enter then runs the line
		private int m_picked = -1;
		private bool m_suggestionsDismissed;

		// Where Up and Down are in the history, -1 for the line being typed, and that line, kept while browsing
		private int m_historyIndex = -1;
		private string m_draft;

		private bool m_wasFocused;

		// Up and Down also move the field's caret to the start or end of the line, repeating while held, before or
		// after this update runs. So while either is held, and the frame after, the caret is held where it was
		private int m_holdCaretUntilFrame = -1;
		private int m_scrollToEndFrames;
		private int m_refocusAfterFrame = -1;

		public DevConsole(DevBoardUI board)
		{
			m_board = board;
			m_commands = DevBoard.Instance.Commands;
			m_history = LoadHistory();

			m_commands.Add("clear", (Action) Clear, "Clears the console");
		}

		public void Toggle()
		{
			if (IsOpen) {
				Close();
			} else {
				Open();
			}
		}

		public void Open()
		{
			if (IsOpen) {
				m_input.Focus(false);
				return;
			}

			// The console floats on the panels' canvas, which only exists once there's a panel
			if (m_board.PopupLayer == null) {
				m_board.Panel(DevBoardUI.MAIN_PANEL_ID);
			}

			var layer = m_board.PopupLayer;

			if (layer == null) {
				return;
			}

			Build(layer);

			RefreshOutput();

			m_scrollToEndFrames = SCROLL_TO_END_FRAMES;
			UpdateCompletion();
			m_input.Focus(false);

			// Turns the canvas on if DevBoard is hidden
			m_board.ApplyVisibility();
		}

		public void Close()
		{
			if (!IsOpen) {
				return;
			}

			ContainerWidget.DestroyWidget(m_root.gameObject);
			m_root = null;
			m_scroll = null;
			m_suggestionBox = null;
			m_outputField = null;
			m_hint = null;
			m_input = null;
			m_suggestionLabels.Clear();
			m_refocusAfterFrame = -1;

			// Turns the canvas back off if DevBoard is hidden
			m_board.ApplyVisibility();
		}

		/// <summary>
		/// Called every frame: reads the line as it's typed and handles the keys.
		/// </summary>
		public void Update()
		{
			if (!IsOpen) {
				return;
			}

			if (m_refocusAfterFrame >= 0 && Time.frameCount > m_refocusAfterFrame) {
				m_refocusAfterFrame = -1;
				m_input.Focus(false);
			}

			if (KeyInput.UpOrDownHeld) {
				m_holdCaretUntilFrame = Time.frameCount + 1;
			}

			ReadLine();
			m_outputField.FitToText();
			FitHeight();

			// After the sizing above, and again next frame once the layout has settled on it
			if (m_scrollToEndFrames > 0) {
				m_scrollToEndFrames--;
				ScrollToEnd();
			}

			PlaceSuggestionBox();

			// Selecting output takes the keyboard from the line. Enter or Escape hands it back
			if (m_outputField.isFocused && (KeyInput.Enter || KeyInput.Escape)) {
				m_input.Focus(false);
				return;
			}

			// The field stops being focused in the same frame Enter is pressed, so last frame counts too
			var focused = m_input.IsFocused;
			var wasFocused = m_wasFocused;
			m_wasFocused = focused;

			if (!focused && !wasFocused) {
				return;
			}

			if (KeyInput.Escape) {
				if (HasSuggestions) {
					m_suggestionsDismissed = true;
					m_picked = -1;
					ShowCompletion();
				} else {
					Close();
				}

				return;
			}

			if (KeyInput.Enter) {
				if (m_picked >= 0 && HasSuggestions) {
					Accept(m_completion.Suggestions[m_picked]);
				} else {
					Run();
				}

				Refocus();
				return;
			}

			if (KeyInput.Up) {
				if (HasSuggestions) {
					MovePick(-1);
				} else {
					BrowseHistory(1);
				}
			} else if (KeyInput.Down) {
				if (HasSuggestions) {
					MovePick(1);
				} else {
					BrowseHistory(-1);
				}
			}
		}

		/// <summary>
		/// Sizes the console to what it holds, up to a fraction of the screen. Past that the output scrolls.
		/// </summary>
		private void FitHeight()
		{
			var rect = (RectTransform) m_root.transform;
			var max = ((RectTransform) rect.parent).rect.height * HEIGHT_FRACTION;
			var height = Mathf.Min(LayoutUtility.GetPreferredHeight(rect), max);

			if (!Mathf.Approximately(rect.sizeDelta.y, height)) {
				rect.sizeDelta = new Vector2(0f, height);
			}
		}

		private bool HasSuggestions => !m_suggestionsDismissed && m_completion.Suggestions.Count > 0;

		private void Build(RectTransform layer)
		{
			m_root = UnityEngine.Object.Instantiate(m_board.Get(WidgetPrefabs.Console), layer, false);
			FitHeight();

			m_scroll = m_root.Scroll.WithoutSavedState();
			m_outputField = m_root.Output;
			m_hint = m_root.Hint;
			m_hint.Monospaced = true;
			m_suggestionBox = m_root.Suggestions;

			foreach (var label in m_root.SuggestionLabels) {
				label.Monospaced = true;
				m_suggestionLabels.Add(label);
			}

			m_input = m_root.Input;
			m_input.Initialize(null, new Binding<string>(() => m_line, _ => { }));
			m_input.WithPlaceholder("Type a command. Tab completes, help lists them").WithInputMode(InputMode.Immediate);

			// Tab moves through the suggestions instead of leaving the field
			var tabHandler = m_input.InputField.gameObject.AddComponent<SearchTabHandler>();
			tabHandler.Tabbed = reverse => {
				if (m_completion.Suggestions.Count == 0) {
					return;
				}

				m_suggestionsDismissed = false;

				if (reverse) {
					MovePick(-1);
				} else {
					Accept(m_completion.Suggestions[Mathf.Max(0, m_picked)]);
				}
			};

			m_input.InputField.restoreOriginalTextOnEscape = false;

			// Enter runs the line and Refocus keeps the keyboard in it. The prompt input moving on to the next
			// element would land on the output
			m_input.InputField.EnterMovesNext = false;
		}

		/// <summary>
		/// Sizes the box to its suggestions and stands it on top of the hint, or the line when there's no hint.
		/// </summary>
		private void PlaceSuggestionBox()
		{
			if (m_suggestionBox == null || !m_suggestionBox.gameObject.activeSelf) {
				return;
			}

			var root = (RectTransform) m_root.transform;
			var below = (RectTransform) (m_hint.gameObject.activeSelf ? m_hint.transform : m_input.transform);
			below.GetWorldCorners(s_corners);
			var topLeft = root.InverseTransformPoint(s_corners[1]);

			var box = (RectTransform) m_suggestionBox.transform;
			var bounds = root.rect;
			var x = topLeft.x - bounds.xMin;
			var width = Mathf.Min(LayoutUtility.GetPreferredWidth(box), bounds.width - x);

			box.sizeDelta = new Vector2(width, LayoutUtility.GetPreferredHeight(box));
			box.anchoredPosition = new Vector2(x, topLeft.y - bounds.yMin + SUGGESTION_GAP);
		}

		/// <summary>
		/// Picks up typing and caret moves, and offers suggestions for them.
		/// </summary>
		private void ReadLine()
		{
			var field = m_input.InputField;
			var text = field.text ?? string.Empty;
			var caret = field.caretPosition;

			if (text == m_line && caret == m_caret) {
				return;
			}

			if (text == m_line && Time.frameCount <= m_holdCaretUntilFrame) {
				field.caretPosition = m_caret;
				field.stringPosition = m_caret;
				return;
			}

			var typed = text != m_line;
			m_line = text;
			m_caret = caret;

			if (typed) {
				m_historyIndex = -1;
				m_suggestionsDismissed = false;
			}

			UpdateCompletion();
		}

		private void SetLine(string line, int caret)
		{
			m_line = line;
			m_caret = caret;

			var field = m_input.InputField;
			field.SetTextWithoutNotify(line);
			field.caretPosition = caret;
			field.stringPosition = caret;

			UpdateCompletion();
		}

		private void UpdateCompletion()
		{
			m_completion = m_line.Length == 0 ? CommandCompletion.Empty : m_commands.Complete(m_line, m_caret);
			m_picked = -1;
			ShowCompletion();
		}

		private void ShowCompletion()
		{
			var suggestions = m_completion.Suggestions;
			var shown = HasSuggestions ? Mathf.Min(suggestions.Count, m_suggestionLabels.Count) : 0;

			// Keeps the pick in view when it's past the first few
			var first = m_picked >= shown ? m_picked - shown + 1 : 0;

			// Tab takes the picked suggestion, or the first, so that one looks picked
			var target = Mathf.Max(0, m_picked);

			// Names and arguments padded to the longest shown, so each lines up in a column (the text is monospace)
			var nameWidth = 0;
			var detailWidth = 0;

			for (var i = 0; i < shown; i++) {
				nameWidth = Mathf.Max(nameWidth, suggestions[first + i].Label.Length);
				detailWidth = Mathf.Max(detailWidth, suggestions[first + i].Detail?.Length ?? 0);
			}

			for (var i = 0; i < m_suggestionLabels.Count; i++) {
				var label = m_suggestionLabels[i];
				label.gameObject.SetActive(i < shown);

				if (i < shown) {
					var index = first + i;
					var suggestion = suggestions[index];
					var hasMore = suggestion.Detail != null || suggestion.Description != null;
					var name = hasMore ? suggestion.Label.PadRight(nameWidth) : suggestion.Label;
					var text = $"<noparse>{name}</noparse>";

					if (index == target) {
						text = string.Format(PICKED, text);
					}

					if (suggestion.Description != null && detailWidth > 0) {
						var detail = (suggestion.Detail ?? string.Empty).PadRight(detailWidth);
						text += "  " + string.Format(DIM, $"<noparse>{detail}</noparse>");
					} else if (suggestion.Detail != null) {
						text += "  " + string.Format(DIM, $"<noparse>{suggestion.Detail}</noparse>");
					}

					if (suggestion.Description != null) {
						text += "  " + string.Format(DIMMER, $"<noparse>{suggestion.Description}</noparse>");
					}

					label.Text = text;
				}
			}

			m_hint.Text = Hint();
			m_hint.gameObject.SetActive(m_hint.Text.Length > 0);
			m_suggestionBox.gameObject.SetActive(shown > 0);
		}

		/// <summary>
		/// What's wrong with the line, or the command's arguments with the one at the caret picked out.
		/// </summary>
		private string Hint()
		{
			if (m_completion.Error != null) {
				return string.Format(ERROR, $"<noparse>{m_completion.Error}</noparse>");
			}

			var command = m_completion.Command;

			if (command == null) {
				return string.Empty;
			}

			var hint = $"<noparse>{command.Name}</noparse>";

			for (var i = 0; i < command.Parameters.Count; i++) {
				var parameter = $"<noparse>{command.Parameters[i]}</noparse>";
				var current = command.ParameterAt(m_completion.ArgumentIndex) == command.Parameters[i];
				hint += " " + (current ? string.Format(PICKED, parameter) : parameter);
			}

			if (command.Description != null) {
				hint += "  " + string.Format(DIMMER, $"<noparse>{command.Description}</noparse>");
			}

			return hint;
		}

		private void MovePick(int delta)
		{
			var count = m_completion.Suggestions.Count;

			if (count == 0) {
				return;
			}

			m_picked = m_picked < 0
				? delta > 0 ? 0 : count - 1
				: (m_picked + delta + count) % count;

			ShowCompletion();
		}

		private void Accept(CommandSuggestion suggestion)
		{
			var line = suggestion.Apply(m_line, out var caret);
			SetLine(line, caret);
		}

		private void Run()
		{
			var line = m_line.Trim();

			if (line.Length == 0) {
				return;
			}

			Remember(line);

			// Echoed first, so a command that clears the output clears its own line too
			Print(string.Format(ECHO, line));
			var result = m_commands.Run(line);

			foreach (var output in result.Output) {
				Print(output.IsError ? string.Format(ERROR, output.Text) : output.Text);
			}

			SetLine(string.Empty, 0);
		}

		private void Print(string line)
		{
			m_output.Add(line);

			if (m_output.Count > MAX_OUTPUT) {
				m_output.RemoveAt(0);
			}

			if (IsOpen) {
				RefreshOutput();
				m_scrollToEndFrames = SCROLL_TO_END_FRAMES;
			}
		}

		private void RefreshOutput()
		{
			m_outputField.SetLines(m_output);
		}

		private void Clear()
		{
			m_output.Clear();

			if (m_outputField != null) {
				RefreshOutput();
			}
		}

		private void ScrollToEnd()
		{
			if (m_scroll != null && m_scroll.TryGetComponent(out ScrollRect scrollRect)) {
				Canvas.ForceUpdateCanvases();
				scrollRect.verticalNormalizedPosition = 0f;
			}
		}

		/// <summary>
		/// Puts the keyboard back in the line next frame. Enter deactivates the field later in the frame it's
		/// pressed, so focusing straight away would be undone.
		/// </summary>
		private void Refocus()
		{
			m_refocusAfterFrame = Time.frameCount;
		}

		/// <summary>
		/// Up goes back through history (direction 1), Down forward (-1), back to the line being typed.
		/// </summary>
		private void BrowseHistory(int direction)
		{
			if (m_history.Count == 0) {
				return;
			}

			if (m_historyIndex < 0) {
				m_draft = m_line;
			}

			var index = Mathf.Clamp(m_historyIndex + direction, -1, m_history.Count - 1);

			if (index == m_historyIndex) {
				return;
			}

			m_historyIndex = index;

			// Newest last in the list, so index 0 is the most recent
			var line = index < 0 ? m_draft ?? string.Empty : m_history[m_history.Count - 1 - index];
			SetLine(line, line.Length);

			// Browsing keeps going until something's typed, so suggestions stay out of the way
			m_suggestionsDismissed = true;
			ShowCompletion();
		}

		private void Remember(string line)
		{
			m_history.Remove(line);
			m_history.Add(line);

			if (m_history.Count > MAX_HISTORY) {
				m_history.RemoveAt(0);
			}

			m_historyIndex = -1;
			SaveHistory();
		}

		[Serializable]
		private class History
		{
			public List<string> Lines = new();
		}

		private static List<string> LoadHistory()
		{
			try {
				var json = PlayerPrefs.GetString(HISTORY_KEY, null);

				if (!string.IsNullOrEmpty(json)) {
					return JsonUtility.FromJson<History>(json)?.Lines ?? new List<string>();
				}
			} catch (Exception exception) {
				Debug.LogWarning($"[DevBoard] Couldn't read the console history: {exception.Message}");
			}

			return new List<string>();
		}

		private void SaveHistory()
		{
			PlayerPrefs.SetString(HISTORY_KEY, JsonUtility.ToJson(new History { Lines = m_history }));
			PlayerPrefs.Save();
		}
	}
}
