using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Common.Terminal.Commands
{
    public interface ICommandSource { }

    [Serializable]
    public class CommandMessage
    {
        public string Message;
        public LogType LogType;

        public string ToConsoleMessage()
        {
            switch (LogType) {
                case LogType.Exception:
                case LogType.Error:
                case LogType.Assert:
                    return Message.Error();
                case LogType.Warning:
                    return Message.Warning();
                case LogType.Log:
                    return Message;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public CommandMessage(string message, LogType logType = LogType.Log)
        {
            Message = message;
            LogType = logType;
        }
    }

    public class CommandStore : Singleton<CommandStore>
    {
        public IReadOnlyList<CommandDescriptor> Commands => m_commands;
        public IReadOnlyList<CommandMessage> Messages => m_messages;
        public IReadOnlyList<string> CommandHistory => m_commandHistory;

        public event Action MessagesChanged;
        public event Action CommandExecuted;

        private readonly List<CommandDescriptor> m_commands = new List<CommandDescriptor>();
        private readonly List<CommandMessage> m_messages = new List<CommandMessage>();
        private readonly List<string> m_commandHistory = new List<string>();
        
        private readonly int m_maxCommandHistory = 10;

        public void Initialize(TerminalPrefs terminalPrefs)
        {
            if (terminalPrefs?.CommandHistory != null) {
                m_commandHistory.AddRange(terminalPrefs.CommandHistory);
            }
        }

        public void AddMessage(string message, LogType logType = LogType.Log)
        {
            AddMessage(new CommandMessage(message, logType));
        }

        public void AddError(string message)
        {
            AddMessage(message, LogType.Error);
        }

        public void AddMessage(CommandMessage message)
        {
            m_messages.Add(new CommandMessage(""));
            m_messages.Add(message);

            MessagesChanged?.Invoke();
        }

        public void ClearMessages()
        {
            m_messages.Clear();
            MessagesChanged?.Invoke();
        }
        
        public void ClearHistory()
        {
            m_commandHistory.Clear();
        }

        public void AddCommandSource(ICommandSource commandSource)
        {
            var commandType = commandSource.GetType();

            var methods = commandType.GetMethods()
                .Where(x => x.GetCustomAttribute<CommandAttribute>() != null)
                .ToArray();

            if (methods == null || methods.Length == 0) {
                throw new Exception($"CommandSource with no methods with CommandAttribute: [{commandType.FullName}]");
            }

            foreach (var method in methods) {
                var descriptor = new CommandDescriptor(commandSource, method);
                m_commands.Add(descriptor);
            }

            m_commands.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));
        }

        public void Execute(string input)
        {
            m_commandHistory.Add(input);

            while (m_commandHistory.Count >= m_maxCommandHistory) {
                m_commandHistory.RemoveAt(0);
            }

            var tokens = SplitArgs(input).ToArray();
            var commandId = tokens[0];

            var command = m_commands.FirstOrDefault(x => x.Id == commandId);

            if (command != null) {
                AddMessage($"> {input}".Gray());
                try {
                    if (tokens.Length == 2 && (tokens[1] == "-h" || tokens[1] == "-help")) {
                        AddMessage(command.ToDescription());
                    } else {
                        command.Invoke(tokens.Skip(1).ToArray(), this);
                    }
                } catch (Exception ex) {
                    AddError(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            } else {
                AddError($"> Unknown command [{commandId}]");
            }

            CommandExecuted?.Invoke();
        }

        private static IEnumerable<string> SplitArgs(string commandLine)
        {
            var result = new StringBuilder();

            var quoted = false;
            var escaped = false;
            var started = false;
            var allowCaret = false;

            for (var i = 0; i < commandLine.Length; i++) {
                var chr = commandLine[i];

                if (chr == '^' && !quoted) {
                    if (allowCaret) {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                        allowCaret = false;
                    } else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^') {
                        allowCaret = true;
                    } else if (i + 1 == commandLine.Length) {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                    }
                } else if (escaped) {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                } else if (chr == '"') {
                    quoted = !quoted;
                    started = true;
                } else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"') {
                    escaped = true;
                } else if (chr == ' ' && !quoted) {
                    if (started) {
                        yield return result.ToString();
                    }

                    result.Clear();
                    started = false;
                } else {
                    result.Append(chr);
                    started = true;
                }
            }

            if (started) {
                yield return result.ToString();
            }
        }
    }
}