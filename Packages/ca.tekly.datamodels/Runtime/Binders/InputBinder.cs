using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DataModels.Binders
{
    public class InputBinder : BasicValueBinder<string>
    {
        [FormerlySerializedAs("InputField")] [SerializeField] private TMP_InputField m_inputField;
        
        private void Awake()
        {
            m_inputField.onValueChanged.AddListener(OnValueChanged);
        }
        
        protected override void BindValue(string value)
        {
            m_inputField.SetTextWithoutNotify(value);
        }

        private void OnValueChanged(string value)
        {
            m_model.Value = value;
        }
    }
}