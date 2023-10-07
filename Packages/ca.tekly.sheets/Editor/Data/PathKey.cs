using System;
using System.Linq;

namespace Tekly.Sheets.Data
{
    public class PathKey : IComparable<PathKey>
    {
        public readonly int Index;
        public readonly string Key;

        public readonly bool IsNumber;

        private readonly int m_hashCode;

        public PathKey(string key)
        {
            if (int.TryParse(key, out Index)) {
                Key = null;
                m_hashCode = Index;
                IsNumber = true;
            } else {
                Index = -1;
                Key = key;

                m_hashCode = key.GetHashCode();
            }
        }

        public PathKey(int key)
        {
            Index = key;
            m_hashCode = key;
            IsNumber = true;
        }

        public static PathKey[] ParseHeader(string header)
        {
            // ':' and '.' indicate an object property 
            header = header.Replace(':', '.');

            // '|' indicates an array
            // We replace '|' with '.0.' to make it easy to update the PathKey with proper indices later
            // in the SheetParser
            header = header.Replace("|", ".0.");

            if (header.EndsWith(".")) {
                header = header.Substring(0, header.Length - 1);
            }

            // At this point we've ensured all separators are '.'
            return header.Split('.').Select(x => new PathKey(x)).ToArray();
        }

        public override string ToString()
        {
            return Key ?? Index.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return m_hashCode == ((PathKey)obj).m_hashCode;
        }

        public override int GetHashCode()
        {
            return m_hashCode;
        }

        public int CompareTo(PathKey other)
        {
            if (ReferenceEquals(this, other)) {
                return 0;
            }

            if (ReferenceEquals(null, other)) {
                return 1;
            }

            if (IsNumber) {
                return Index.CompareTo(other.Index);
            }

            return string.Compare(Key, other.Key, StringComparison.Ordinal);
        }
    }
}