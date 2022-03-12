using System;

namespace Tekly.Injectors
{
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