using TMPro;
using UnityEngine;

namespace Tekly.Localizations
{
    public class TextLocalizer : MonoBehaviour
    {
        public string Id;
        public TMP_Text Text;
        
        private readonly Localizer m_localizer = Localizer.Instance;

        private void OnEnable()
        {
            Text.text = m_localizer.Localize(Id);
        }
    }
}