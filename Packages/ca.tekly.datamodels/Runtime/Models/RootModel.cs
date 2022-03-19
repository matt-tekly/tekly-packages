using Tekly.Common.Utils;

namespace Tekly.DataModels.Models
{
    public class RootModel : ObjectModel
    {
        public static RootModel Instance { get; private set; }

        static RootModel()
        {
            UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
            UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
        }

        private static void Init()
        {
            Instance = new RootModel();
        }

        private static void Reset()
        {
            Instance = null;
        }
    }
}