using System;

namespace Tekly.Injectors
{
    public class DependencyCycleException : Exception
    {
        public DependencyCycleException(Type type) : base("Type dependency cycle detected in " + type.FullName)
        {
            
        }
    }
}