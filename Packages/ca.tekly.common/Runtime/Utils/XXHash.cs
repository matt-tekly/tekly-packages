/*
    Original C Implementation Copyright (C) 2012-2014, Yann Collet. (https://code.google.com/p/xxhash/)
    BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are
    met:

        * Redistributions of source code must retain the above copyright
          notice, this list of conditions and the following disclaimer.
        * Redistributions in binary form must reproduce the above
          copyright notice, this list of conditions and the following
          disclaimer in the documentation and/or other materials provided
          with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
    A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
    OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
    DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
    THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using UnityEngine;

namespace Tekly.Common.Utils
{
    public static class XXHash
    {
        private const uint PRIME32_2 = 2246822519U;
        private const uint PRIME32_3 = 3266489917U;
        private const uint PRIME32_4 = 668265263U;
        private const uint PRIME32_5 = 374761393U;

        private const float TAU = 6.28318530718f;

        public static uint Calculate(uint seed, uint data)
        {
            var h32 = seed + PRIME32_5;
            h32 += 4U;
            h32 += data * PRIME32_3;
            h32 = RotateLeft32(h32, 17) * PRIME32_4;
            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;

            return h32;
        }

        public static int Int(uint seed, uint data, int min, int max)
        {
            return (int) (Calculate(seed, data) >> 1) % (max - min) + min;
        }

        public static float Float(uint seed, uint data, float min, float max)
        {
            return Calculate(seed, data) / (float) uint.MaxValue * (max - min) + min;
        }

        public static Vector3 Direction(uint seed, uint data)
        {
            var theta = Float(seed, data, 0, TAU);
            var phi = Mathf.Acos(Float(seed, data + 0x10000000, -1, 1));
            
            var sinPhi = Mathf.Sin(phi);
            
            var x = sinPhi * Mathf.Cos(theta);
            var y = sinPhi * Mathf.Sin(theta);
            var z = Mathf.Cos(phi);

            return new Vector3(x, y, z);
        }

        public static Quaternion Rotation(uint seed, uint data)
        {
            var v = Direction(seed, data);
            var phi = Float(seed, data + 0x20000000, 0, TAU);
            var q1 = Quaternion.AngleAxis(phi, Vector3.forward);
            var q2 = Quaternion.FromToRotation(Vector3.forward, v);

            return q1 * q2;
        }

        private static uint RotateLeft32(uint x, int r)
        {
            return (x << r) | (x >> 32 - r);
        }
    }
}