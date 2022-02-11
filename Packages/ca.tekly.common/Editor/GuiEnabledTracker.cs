using System;
using UnityEngine;

namespace Tekly.Common
{
    public readonly struct GuiEnabledTracker : IDisposable
    {
        private readonly bool m_enabledState;

        public GuiEnabledTracker(bool enabled)
        {
            m_enabledState = GUI.enabled;
            GUI.enabled = enabled;
        }

        public void Dispose()
        {
            GUI.enabled = m_enabledState;
        }
    }
}