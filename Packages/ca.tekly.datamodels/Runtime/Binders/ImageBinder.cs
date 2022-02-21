using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DataModels.Binders
{
    public class ImageBinder : Binder
    {
        public ModelRef Key;
        public Image Image;

        private IDisposable m_disposable;
        
        public override void Bind(BinderContainer container)
        {
            if (container.TryGet(Key.Path, out SpriteValueModel spriteModel)) {
                m_disposable?.Dispose();
                m_disposable = spriteModel.Subscribe(BindSprite);
            }
        }

        private void BindSprite(Sprite sprite)
        {
            Image.sprite = sprite;
        }
        
        private void OnDestroy()
        {
            m_disposable?.Dispose();
        }
    }
}