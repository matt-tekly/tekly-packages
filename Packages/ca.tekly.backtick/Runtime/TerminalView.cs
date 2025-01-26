using Tekly.Backtick.Commands;
using UnityEngine;

namespace Tekly.Backtick
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