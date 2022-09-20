using System;
using Tekly.Common.Utils;

namespace Tekly.Balance
{
    public class BalanceObject : OverridableData
    {
        [NonSerialized] private string m_id;
        public string Id => m_id ??= name;
    }
}
