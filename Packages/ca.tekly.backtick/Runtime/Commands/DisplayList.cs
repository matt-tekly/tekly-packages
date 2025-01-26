using System.Collections.Generic;
using System.Linq;

namespace Tekly.Backtick.Commands
{
    public class DisplayItem
    {
        public readonly string Name;
        public readonly string Value;
        
        public DisplayItem(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public DisplayItem(string name, float value)
        {
            Name = name;
            Value = value.ToString("0.00");
        }

        public DisplayItem(string name, int value)
        {
            Name = name;
            Value = value.ToString();
        }

        public DisplayItem(string name, bool value)
        {
            Name = name;
            Value = value.ToString();
        }
    }
    
    /// <summary>
    /// Helps display a list of items to be displayed in the terminal
    /// </summary>
    public class DisplayList
    {
        private readonly List<DisplayItem> m_items = new List<DisplayItem>();
        
        public void Add(string name, string value)
        {
            m_items.Add(new DisplayItem(name, value));
        }

        public void Add(string name, float value)
        {
            m_items.Add(new DisplayItem(name, value));
        }

        public void Add(string name, int value)
        {
            m_items.Add(new DisplayItem(name, value));
        }

        public void Add(string name, bool value)
        {
            m_items.Add(new DisplayItem(name, value));
        }

        public string ToString(bool aligned = true, bool padRight = false)
        {
            if (aligned) {
                if (padRight) {
                    var longestName = m_items.Max(x => x.Name.Length);
                    var longestValue = m_items.Max(x => x.Value.Length);
                    return string.Join("\n", m_items.Select(x => $"{x.Name.PadRight(longestName)} {x.Value.PadLeft(longestValue).Gray()}"));
                } else {
                    var longestName = m_items.Max(x => x.Name.Length);
                    return string.Join("\n", m_items.Select(x => $"{x.Name.PadRight(longestName)} {x.Value.Gray()}"));    
                }
            } 
            
            return string.Join("\n", m_items.Select(x => $"{x.Name} {x.Value.Gray()}"));
        }
    }
}