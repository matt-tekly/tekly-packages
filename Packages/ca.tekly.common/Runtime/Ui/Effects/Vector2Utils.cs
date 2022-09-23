using UnityEngine;

namespace Tekly.Common.Ui.Effects
{
    public static class Vector2Utils
    {
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);

            var tx = v.x;
            var ty = v.y;
            
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }
        
        public static Vector2 Rotate90(this Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }
        
        public static Vector2 UnitRotated(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
  
            return new Vector2 {
                x = -Mathf.Sin(radians),
                y = Mathf.Cos(radians)
            };
        }
    }
}