using UnityEngine;

namespace Tekly.Balance
{
    public class BalanceObject : ScriptableObject
    {
        private string m_id;
        public string Id => m_id ??= name;
    }
}
