using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Tekly.Common.Utils;
using Tekly.Terminal;
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
        private readonly List<CommandDescriptor> m_commands = new List<CommandDescriptor>();

        public readonly List<CommandMessage> Messages = new List<CommandMessage>();
        public readonly List<string> CommandHistory = new List<string>();

        public event Action MessagesChanged;
        public event Action CommandExecuted;

        private int m_maxCommandHistory = 10;

        public void Initialize(TerminalPrefs terminalPrefs)
        {
            if (terminalPrefs != null && terminalPrefs.CommandHistory != null) {
                CommandHistory.AddRange(terminalPrefs.CommandHistory);
            }
        }

        public void AddMessage(string message)
        {
            Messages.Add(new CommandMessage(""));
            Messages.Add(new CommandMessage(message));
            MessagesChanged?.Invoke();
        }

        public void AddMessage(CommandMessage message)
        {
            Messages.Add(new CommandMessage(""));
            Messages.Add(message);
            MessagesChanged?.Invoke();
        }

        public void AddError(string message)
        {
            Messages.Add(new CommandMessage(""));
            Messages.Add(new CommandMessage(message, LogType.Error));
            MessagesChanged?.Invoke();
        }

        public void ClearMessages()
        {
            Messages.Clear();
            MessagesChanged?.Invoke();
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
            CommandHistory.Add(input);

            while (CommandHistory.Count >= m_maxCommandHistory) {
                CommandHistory.RemoveAt(0);
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
                }
                catch (Exception ex) {
                    AddError(ex.ToString());
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