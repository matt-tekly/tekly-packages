using TMPro;
using UnityEngine;

namespace Tekly.Localizations
{
    public class TextLocalizer : MonoBehaviour
    {
        public string Id;
        public TMP_Text Text;
        
        private void OnEnable()
        {
            Text.text = Localizer.Instance.Localize(Id);
        }
    }
}