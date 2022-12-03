using UnityEngine;

namespace Tekly.Common.Utils
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