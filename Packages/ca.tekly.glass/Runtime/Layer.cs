using UnityEngine;

namespace Tekly.Glass
{
    public class Layer : MonoBehaviour
    {
        public string Id => name;
        public RectTransform Container => GetComponent<RectTransform>();

        public void Add(GameObject panel)
        {
            var source = Container;
            panel.transform.SetParent(Container, true);
            ApplySourceRect(panel.GetComponent<RectTransform>(), source);
        }

        public void Remove(GameObject panel)
        {
            gameObject.transform.parent = null;
        }
        
        public void ApplySourceRect(RectTransform localTransform, RectTransform sourceTransform)
        {
            localTransform.localScale = sourceTransform.localScale;
            localTransform.rotation = sourceTransform.localRotation;

            localTransform.anchorMin = sourceTransform.anchorMin;
            localTransform.anchorMax = sourceTransform.anchorMax;
            localTransform.anchoredPosition = sourceTransform.anchoredPosition;
            localTransform.sizeDelta = sourceTransform.sizeDelta;
        }
    }
}