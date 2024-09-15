using UnityEngine;

namespace Tekly.EditorUtils.Attributes
{
    public class PolymorphicAttribute : PropertyAttribute
    {
        public string Title { get; }

        public PolymorphicAttribute(string title = "Types")
        {
            Title = title;
        }
    }
}