// Sets the CSS cursor on the Unity canvas, so the browser shows its own cursor. An empty string puts back the default.
mergeInto(LibraryManager.library, {
	DevBoard_SetCursor: function (cursor) {
		var canvas = Module.canvas || document.querySelector("canvas");

		if (canvas) {
			canvas.style.cursor = UTF8ToString(cursor);
		}
	}
});
