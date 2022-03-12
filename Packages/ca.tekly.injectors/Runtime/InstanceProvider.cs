using System;

namespace Tekly.Injectors
{
    /// <summary>
    /// Provides an instance of the BaseType
    /// </summary>
    public interface IInstanceProvider
    {
        object Instance { get; }    
        Type BaseType { get; }
    }
    
    public class InstanceProvider : IInstanceProvider
    {
        public Type BaseType { get; private set; }
        public object Instance { get; private set; }

        public static InstanceProvider Create<T>(T instance)
        {
            return new InstanceProvider {
                BaseType = typeof(T),
                Instance = instance
            };
        }
    }
}