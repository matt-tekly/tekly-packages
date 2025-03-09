using Tekly.DebugKit.Utils;

namespace Tekly.DebugKit
{
	public class DebugKitPreferences
	{
		public readonly BoolPref ScaleInEditor = new BoolPref("debugkit.preferences.scale_in_editor", true);
		public readonly BoolPref ScaleOverrideEnabled = new BoolPref("debugkit.preferences.scale_override.enabled");
		public readonly FloatPref ScaleOverride = new FloatPref("debugkit.preferences.scale_override.value", 1);
	}
}