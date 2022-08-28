using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class BlockingGraphic : Graphic
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
		
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}