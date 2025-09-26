#if USE_DEBUGKIT
using Tekly.DataModels.Models;

namespace Tekly.DataModels.Debugging
{
	public static class DataModelsDebugKitMenu
	{
		
#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Register()
		{
			string value = "";

			IOverridableValue model = null;

			var menu = DebugKit.DebugKit.Instance.Menu("Data Models");

			menu.Style(style => style.minWidth = 300);
			
			var path = menu.TextFieldPersist("Path", "debugkit.datamodel.path");

			menu.Updater(() => {
				var key = ModelKey.Parse(path.Value);
				if (key != null) {
					RootModel.Instance.TryGetModel(key, 0, out model);	
				}
			});
			
			menu.TextField("Value", () => value, v => {
				value = v;
				if (model != null) {
					model.SetOverrideValue(value);
				}
			});
			menu.Property("Model Type", () => {
				if (model != null) {
					return model.GetType().Name;
				}

				return "No model found";
			});
			
			menu.Property("Model value", () => {
				if (model != null) {
					return model.ToDisplayString();
				}

				return "No model found";
			});
		}
	}
}
#endif