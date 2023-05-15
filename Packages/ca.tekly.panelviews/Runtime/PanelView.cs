using System;
using Tekly.Common.Presentables;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.PanelViews
{
    [Serializable]
    public class PanelData { }
    
    public class PanelView : Presentable
    {
        [SerializeField] private string m_id;
        
        public string Id => m_id;

        public string Context { get; private set; }
        public PanelData Data { get; private set; }

        public void Show(string context, PanelData data = null)
        {
            Context = context;
            Data = data;
            
            Show();
        }

        protected override void OnStateChanged()
        {
            PanelViewRegistry.Instance.OnPanelStateChanged(this);
        }
    }
}