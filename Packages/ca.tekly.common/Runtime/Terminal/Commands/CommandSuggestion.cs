using System.Collections.Generic;
using System.Linq;

namespace Tekly.Common.Terminal.Commands
{
    public class CommandSuggestion
    {
        public bool HasInput => m_input != null;
        public bool HasSuggestions => m_matches.Count > 0;
        public CommandDescriptor Suggestion => m_matches[m_suggestIndex];
        
        private string m_input;
        private int m_suggestIndex;
        
        private readonly List<CommandDescriptor> m_matches = new List<CommandDescriptor>();
        private readonly CommandStore m_commandStore;
        
        public CommandSuggestion(CommandStore commandStore)
        {
            m_commandStore = commandStore;
        }

        public void SetInput(string input)
        {
            m_input = input;
            m_matches.AddRange(m_commandStore.Commands.Where(x => x.Id.StartsWith(input)));
        }

        public void Clear()
        {
            m_input = null;
            m_matches.Clear();
            m_suggestIndex = 0;
        }

        public void Increment()
        {
            m_suggestIndex = (m_suggestIndex + 1) % m_matches.Count;
        }
        
        public void Decrement()
        {
            if (m_suggestIndex == 0) {
                m_suggestIndex = m_matches.Count - 1;
            } else {
                m_suggestIndex--;
            }
        }
        
    }
}