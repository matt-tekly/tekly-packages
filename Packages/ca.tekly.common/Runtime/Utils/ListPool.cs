// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;

namespace Tekly.Common.Utils
{
    public class ListPool<T> : ObjectPoolBase<List<T>>
    {
        public static readonly ListPool<T> Instance = new ListPool<T>();
        
        protected override List<T> Allocate()
        {
            return new List<T>();
        }

        protected override void Recycled(List<T> element)
        {
            element.Clear();
        }
    }
}