using System;
using UnityEngine;

namespace Tekly.Localizations
{
    [Serializable]
    public class LocalizationStringData
    {
        public string Id;
        [TextArea] public string Format;
    }

    [CreateAssetMenu(menuName = "Tekly/Localization/Data")]
    public class LocalizationData : ScriptableObject
    {
        public LocalizationStringData[] Strings;
    }
}
