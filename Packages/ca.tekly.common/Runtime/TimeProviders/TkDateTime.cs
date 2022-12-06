using System;
using System.Globalization;
using UnityEngine;

namespace Tekly.Common.TimeProviders
{
    [Serializable]
    public struct TkDateTime : ISerializationCallbackReceiver
    {
        public DateTimeOffset Time;
        [SerializeField] private string m_time;
		
        public void OnBeforeSerialize()
        {
            m_time = Time.ToString(CultureInfo.InvariantCulture);
        }

        public void OnAfterDeserialize()
        {
            DateTimeOffset.TryParse(m_time, out Time);
        }

        public bool Equals(TkDateTime other) => Time.Equals(other.Time);
        public override bool Equals(object obj) => obj is TkDateTime other && Equals(other);
        public override int GetHashCode() => Time.GetHashCode();

        public static TkDateTime Now => DateTimeOffset.Now;
		
        public static implicit operator TkDateTime(DateTimeOffset dateTime) => new TkDateTime {Time = dateTime};
        
        public static bool operator ==(TkDateTime left, TkDateTime right) => left.Time == right.Time;
        public static bool operator >(TkDateTime left, TkDateTime right) => left.Time > right.Time;
        public static bool operator >=(TkDateTime left, TkDateTime right) => left.Time >= right.Time;
        public static bool operator <=(TkDateTime left, TkDateTime right) => left.Time <= right.Time;
        public static bool operator !=(TkDateTime left, TkDateTime right) => left.Time != right.Time;
        public static bool operator <(TkDateTime left, TkDateTime right) => left.Time < right.Time;
        
        public static TkDateTime operator +(TkDateTime left, TimeSpan right) => left.Time + right;
        public static TkDateTime operator -(TkDateTime left, TimeSpan right) => left.Time - right;
    }
}