using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tekly.Backtick.Commands
{
    public class TerminalCommands : ICommandSource
    {
        private readonly CommandStore m_commandStore;
        private readonly TerminalRoot m_terminalRoot;

        public TerminalCommands(CommandStore commandStore, TerminalRoot terminalRoot)
        {
            m_commandStore = commandStore;
            m_terminalRoot = terminalRoot;
        }
        
        [Command("terminal.clear")]
        [Help("Clears the terminal")]
        public void ClearMessages()
        {
            m_commandStore.ClearMessages();
        }

        [Command("terminal.prefs.scale")]
        [Help("Get or set the scale of the terminal")]
        public string TerminalScale(float? scale)
        {
            if (scale.HasValue) {
                m_terminalRoot.SetViewScale(Mathf.Clamp(scale.Value, 0.5f, 3f));
            }

            return m_terminalRoot.GetViewScale().ToString("0.00");
        }

        [Command("terminal.prefs.size")]
        [Help("Get or set the size of the terminal as ratio of the screen size")]
        public string TerminalSize(float? size)
        {
            if (size.HasValue) {
                m_terminalRoot.SetViewSize(Mathf.Clamp01(size.Value));
            }

            return m_terminalRoot.GetViewSize().ToString("0.00");
        }


        [Command("terminal.prefs.reset")]
        [Help("Resets the terminal preferences")]
        public string TerminalReset()
        {
            m_terminalRoot.ResetPreferences();
            return "Preferences Reset";
        }

        [Command("help")]
        [Help("Displays list of commands")]
        public string HelpMessage(string filter = null)
        {
            m_commandStore.AddMessage($"For additional details about a command use {"(command) -h".Gray()}");

            IReadOnlyList<CommandDescriptor> commands;

            if (filter != null) {
                var regex = new Regex(filter, RegexOptions.IgnoreCase);
                commands = m_commandStore.Commands
                    .Where(x => regex.IsMatch(x.Id))
                    .ToList();
            } else {
                commands = m_commandStore.Commands;
            }

            if (commands.Count == 0) {
                return "No commands found".Gray();
            }
        
            var longestCommand = commands.Max(x => x.Id.Length);
            return string.Join("\n", commands.Select(x => x.ToHelpSummary(longestCommand)));
        }
    }
}