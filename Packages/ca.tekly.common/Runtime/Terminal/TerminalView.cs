using Tekly.Common.Terminal.Commands;
using UnityEngine;

namespace Tekly.Common.Terminal
{
    public abstract class TerminalView : MonoBehaviour
    {
        public abstract float GetScale();
        public abstract void SetScale(float scale);
        
        public abstract float GetSize();
        public abstract void SetSize(float size);

        public abstract void Initialize(TerminalRoot container, CommandStore commandStore);
    }
}