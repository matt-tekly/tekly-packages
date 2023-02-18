using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class ImageBinder : Binder
    {
        [FormerlySerializedAs("Image")][SerializeField] private Image m_image;
        [FormerlySerializedAs("Key")][SerializeField] private ModelRef m_key;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(m_key.Path, out SpriteValueModel spriteModel)) {
                m_disposable?.Dispose();
                m_disposable = spriteModel.Subscribe(BindSprite);
            }
        }

        private void BindSprite(Sprite sprite)
        {
            m_image.sprite = sprite;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_disposable?.Dispose();
        }
    }
}