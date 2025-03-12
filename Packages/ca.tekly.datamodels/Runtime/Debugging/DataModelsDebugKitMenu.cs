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
			string path = "";
			string value = "";

			IOverridableValue model = null;

			DebugKit.DebugKit.Instance.Menu("Data Models")
				.Style(style => style.minWidth = 300)
				.TextField("Path", () => path, v => {
					path = v;

					var key = ModelKey.Parse(path);
					RootModel.Instance.TryGetModel(key, 0, out model);
				})
				.TextField("Value", () => value, v => {
					value = v;
					if (model != null) {
						model.SetOverrideValue(value);
					}
				})
				.Property("Model", () => {
					if (model != null) {
						return model.GetType().Name;
					}

					return "No model found";
				});
		}
	}
}
#endif