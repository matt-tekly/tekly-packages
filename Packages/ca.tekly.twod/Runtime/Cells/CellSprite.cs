using UnityEngine;

namespace Tekly.TwoD.Cells
{
    public class CellSprite : ScriptableObject
    {
        public Sprite Icon;
        public CellAnimation[] Animations;
        public SpritePixelData PixelData;

        public CellAnimation Get(string animName)
        {
            foreach (var spriteAnimation in Animations) {
                if (spriteAnimation.name == animName) {
                    return spriteAnimation;
                }
            }

            return null;
        }
    }
}
