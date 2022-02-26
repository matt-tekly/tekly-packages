using System;
using UnityEngine;

namespace Tekly.Balance
{
    public class BalanceObject : ScriptableObject
    {
        [NonSerialized] private string m_id;
        public string Id => m_id ??= name;
    }
}
