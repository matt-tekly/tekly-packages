#if UNITY_EDITOR || !TEKLY_BACKTICK_DISABLE
    #define BACKTICK_ENABLED
#endif

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Tekly.Common.Utils;
using UnityEngine.Scripting;

namespace Tekly.Backtick.Commands
{
#if BACKTICK_ENABLED
    public class CommandAttribute : PreserveAttribute
#else
    public class CommandAttribute : Attribute
#endif
    {
        public readonly string Id;
        
        public CommandAttribute(string id)
        {
            Id = id;
        }
    }
    
    public class HelpAttribute : Attribute
    {
        public readonly string Message;

        public HelpAttribute(string message)
        {
            Message = message;
        }
    }

    public class ParameterDescriptor
    {
        public readonly string Name;
        public readonly Type Type;
        public readonly bool Optional;
        public readonly object DefaultValue;
        public readonly bool Ignore;
        public readonly string TypeName;
        
        public ParameterDescriptor(ParameterInfo parameterInfo)
        {
            Name = parameterInfo.Name;
            Type = parameterInfo.ParameterType;
            Optional = parameterInfo.IsOptional || TypeUtility.IsNullable(parameterInfo.ParameterType);
            TypeName = TypeUtility.HumanName(Type);
            
            if (parameterInfo.DefaultValue == Convert.DBNull) {
                DefaultValue = null;    
            } else {
                DefaultValue = parameterInfo.DefaultValue;
            }
            
            Ignore = Type == typeof(CommandStore);
        }

        public override string ToString()
        {
            return Optional ? $"[{TypeName} {Name}]" : $"<{TypeName} {Name}>";
        }
    }
    
    public class CommandDescriptor
    {
        public readonly object Source;
        public readonly MethodInfo Method;
        public readonly ParameterDescriptor[] Parameters;
        public readonly string Id;
        public readonly int RequiredParameterCount;
        public readonly string Help;

        public CommandDescriptor(object source, MethodInfo method)
        {
            Source = source;
            Method = method;
            
            var commandAttribute = Method.GetCustomAttribute<CommandAttribute>();
            Id = commandAttribute.Id;

            Parameters = Method.GetParameters().Select(x => new ParameterDescriptor(x)).ToArray();
            RequiredParameterCount = Parameters.Count(x => !x.Optional && x.Type != typeof(CommandStore));
            
            var helpAttribute = Method.GetCustomAttribute<HelpAttribute>();
            if (helpAttribute != null) {
                Help = helpAttribute.Message;
            }
        }

        public string ToHelpSummary(int longestCommand)
        {
            return $"{Id.PadRight(longestCommand).Emphasize()} {Help.Gray()}";
        }
        
        public string ToDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Help);
            sb.AppendLine();
            sb.Append($"Usage: {Id.Emphasize()} ");
            sb.Append(string.Join(" ", Parameters.Where(x => !x.Ignore).Select(x => x.ToString())).Gray());

            foreach (var parameter in Parameters) {
                if (parameter.Type.IsEnum) {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine($"{parameter.Type.Name} options:");
                    sb.AppendLine();

                    var values = Enum.GetValues(parameter.Type)
                        .Cast<object>()
                        .Select(x => "\t" + x.ToString().Gray());
                    
                    sb.Append(string.Join("\n", values));
                }
            }
            
            return sb.ToString();
        }

        public void Invoke(string[] parameters, CommandStore commandStore)
        {
            var invokeParams = new object[Parameters.Length];

            for (var index = 0; index < Parameters.Length; index++) {
                var parameterDescriptor = Parameters[index];
                if (index < parameters.Length) {
                    var value = parameters[index];
                    invokeParams[index] = TypeUtility.Parse(parameterDescriptor.Type, value);    
                } else {
                    if (parameterDescriptor.Type == typeof(CommandStore)) {
                        invokeParams[index] = commandStore;
                    } else if(parameterDescriptor.Optional) {
                        invokeParams[index] = parameterDescriptor.DefaultValue;
                    } else {
                        commandStore.AddError($"Not enough parameters. Expected [{RequiredParameterCount}] received [{parameters.Length}]");
                    }
                }
            }

            var returnValue = Method.Invoke(Source, invokeParams);
            if (returnValue is CommandMessage message) {
                commandStore.AddMessage(message);
            }
            
            if (returnValue is string strMessage) {
                commandStore.AddMessage(strMessage);
            }
        }
    }
}