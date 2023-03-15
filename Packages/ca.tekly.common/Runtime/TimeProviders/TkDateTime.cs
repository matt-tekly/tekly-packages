using System;
using System.Globalization;
using UnityEngine;

namespace Tekly.Common.TimeProviders
{
    [Serializable]
    public struct TkDateTime : ISerializationCallbackReceiver, IComparable, IComparable<TkDateTime>, IEquatable<TkDateTime>
    {
        public const string FORMAT = "yyyy'/'MM'/'dd'T'HH':'mm':'sszzz";
        
        public DateTimeOffset Time { get; private set; }
        
        [SerializeField] private string m_time;
        
        public int Millisecond => Time.Millisecond;
        public int Second => Time.Second;
        public int Minute => Time.Minute;
        public int Hour => Time.Hour;
        public int Day => Time.Day;
        public int Month => Time.Month;
        
        public TkDateTime AddMilliseconds(double milliseconds) => Time.AddMilliseconds(milliseconds);
        public TkDateTime AddSeconds(double seconds) => Time.AddSeconds(seconds);
        public TkDateTime AddMinutes(double minutes) => Time.AddMinutes(minutes);
        public TkDateTime AddHours(double hours) => Time.AddHours(hours);
        public TkDateTime AddDays(double days) => Time.AddDays(days);
        public TkDateTime AddMonths(int months) => Time.AddMonths(months);
        public TkDateTime AddYears(int months) => Time.AddYears(months);

        public TkDateTime Add(TimeSpan timeSpan) => Time.Add(timeSpan);
        public TkDateTime Subtract(TimeSpan timeSpan) => Time.Subtract(timeSpan);

        public override string ToString() => Time.ToString(FORMAT, CultureInfo.InvariantCulture);

        public void OnBeforeSerialize() => m_time = Time.ToString(FORMAT, CultureInfo.InvariantCulture);
        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_time)) {
                return;
            }
            
            if (DateTimeOffset.TryParseExact(m_time, FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) {
                Time = time;
            }
        }

        public bool Equals(TkDateTime other) => Time.Equals(other.Time);
        public int CompareTo(TkDateTime other) => Time.CompareTo(other.Time);
        public override bool Equals(object obj) => obj is TkDateTime other && Equals(other);
        public override int GetHashCode() => Time.GetHashCode();

        public int CompareTo(object obj)
        {
            if (obj is TkDateTime other) {
                return Time.CompareTo(other.Time);
            }

            return obj == null ? 1 : 0;
        }

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
        public static TimeSpan operator -(TkDateTime left, TkDateTime right) => left.Time - right.Time;
    }
}