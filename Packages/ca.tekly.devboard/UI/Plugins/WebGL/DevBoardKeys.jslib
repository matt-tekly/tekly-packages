// Keeps the browser from taking Ctrl+F / Cmd+F (find in page) while DevBoard has focus, so it reaches DevBoard's
// own search. The browser decides during the keydown event, before Unity's next frame, so C# sets the flag ahead of
// time. The event still reaches Unity: only the browser's default action is stopped.
mergeInto(LibraryManager.library, {
	DevBoard_SetBlockFind: function (block) {
		if (!Module.devBoardKeys) {
			Module.devBoardKeys = { blockFind: false };

			window.addEventListener("keydown", function (e) {
				if (Module.devBoardKeys.blockFind && (e.ctrlKey || e.metaKey) && !e.altKey && e.code === "KeyF") {
					e.preventDefault();
				}
			}, true);
		}

		Module.devBoardKeys.blockFind = !!block;
	}
});
