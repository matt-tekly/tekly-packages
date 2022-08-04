using System;
using UnityEngine;

namespace Tekly.Common.Gui
{
    public readonly struct GuiColorTracker : IDisposable
    {
        private readonly Color m_color;

        public GuiColorTracker(Color color)
        {
            m_color = GUI.color;
            GUI.color = color;
        }

        public void Dispose()
        {
            GUI.color = m_color;
        }
    }
}