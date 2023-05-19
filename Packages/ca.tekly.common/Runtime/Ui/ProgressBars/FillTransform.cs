using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProgressBars
{
    /// <summary>
    /// Fills this GameObject to its parents size
    /// </summary>
    [ExecuteAlways]
    public class FillTransform : Filled, ILayoutSelfController
    {
        [SerializeField] private FillDirection m_direction;

        private DrivenRectTransformTracker m_tracker;
        private RectTransform m_rectTransform;
        private RectTransform m_parent;

        private const DrivenTransformProperties DRIVEN_PROPERTIES = DrivenTransformProperties.AnchoredPosition
                                                                    | DrivenTransformProperties.Anchors
                                                                    | DrivenTransformProperties.SizeDelta;
        private RectTransform RectTransform {
            get {
                if (m_rectTransform == null) {
                    m_rectTransform = GetComponent<RectTransform>();
                }

                return m_rectTransform;
            }
        }
		
        private RectTransform ParentTransform {
            get {
                if (m_parent == null) {
                    m_parent = RectTransform.parent as RectTransform;
                }

                return m_parent;
            }
        }

        protected void OnEnable()
        {
            m_tracker.Clear();
            m_tracker.Add(this, RectTransform, DRIVEN_PROPERTIES);

            SetFill(FillAdjusted);

            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
		
        protected void OnDisable()
        {
            m_tracker.Clear();
        }
        
        public void SetLayoutHorizontal()
        {
            UpdateTransform(FillAdjusted);
        }

        public void SetLayoutVertical()
        {
            UpdateTransform(FillAdjusted);
        }
        
        protected override void SetFill(float fill)
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

        private void UpdateTransform(float fill)
        {
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;
            
            switch (m_direction) {
                case FillDirection.LeftToRight: {
                    var width = ParentTransform.rect.width;
                    var rt = RectTransform;
                    rt.offsetMin = new Vector2(-width * (1f - fill), 0);
                    rt.offsetMax = new Vector2(-width * (1f - fill), 0);
                    break;
                }
                case FillDirection.RightToLeft: {
                    var width = ParentTransform.rect.width;
                    var rt = RectTransform;
                    rt.offsetMin = new Vector2(width * (1f - fill), 0);
                    rt.offsetMax = new Vector2(width * (1f - fill), 0);
                    break;
                }
                case FillDirection.TopToBottom: {
                    var height = ParentTransform.rect.height;
                    var rt = RectTransform;
                    rt.offsetMin = new Vector2(0, height * (1f - fill));
                    rt.offsetMax = new Vector2(0, height * (1f - fill));
                    break;
                }
                case FillDirection.BottomToTop: {
                    var height = ParentTransform.rect.height;
                    var rt = RectTransform;
                    rt.offsetMin = new Vector2(0, -height * (1f - fill));
                    rt.offsetMax = new Vector2(0, -height * (1f - fill));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
		
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
#endif
       
    }
}