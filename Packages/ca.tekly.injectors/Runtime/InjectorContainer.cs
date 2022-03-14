// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Injectors
{
    public class InjectorContainer
    {
        private readonly Dictionary<Type, IInstanceProvider> m_instances = new Dictionary<Type, IInstanceProvider>();
        private readonly Dictionary<InstanceId, IInstanceProvider> m_instanceIds = new Dictionary<InstanceId, IInstanceProvider>();
        private static readonly Dictionary<Type, Injector> s_injectableTypes = new Dictionary<Type, Injector>();

        private readonly InjectorContainer m_parent;
        private readonly TkLogger m_logger = TkLogger.Get<InjectorContainer>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Cleanup()
        {
            s_injectableTypes.Clear();
        }

        public InjectorContainer()
        {
            Register(this);
        }
        
        public InjectorContainer(InjectorContainer parent) : this()
        {
            m_parent = parent;
        }
        
        public void Register<T>(T instance)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", typeof(T)));
                return;
            }
            
            m_instances.Add(typeof(T), InstanceProvider.Create(instance));
        }
        
        public void Register<T1, T2>(T1 instance) where T1 : T2
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", typeof(T1)));
                return;
            }
            
            m_instances.Add(typeof(T1), InstanceProvider.Create(instance));
            m_instances.Add(typeof(T2), InstanceProvider.Create(instance));
        }
        
        public void Register<T>(T instance, string id)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", typeof(T)));
                return;
            }
            
            Register(typeof(T), instance, id);
        }
        
        public void Register(Type type, object instance, string id)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", type));
                return;
            }
            
            m_instanceIds.Add(new InstanceId(type, id), InstanceProvider.Create(instance));
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }
        
        public T Get<T>(string id)
        {
            return (T)Get(typeof(T), id);
        }

        public object Get(Type type)
        {
            if (m_instances.TryGetValue(type, out var provider)) {
                return provider.Instance;
            }

            if (m_parent != null) {
                return m_parent.Get(type);
            }

            throw new Exception($"Failed to get instance for type [{type.FullName}]");
        }
        
        public object Get(Type type, string id)
        {
            var instanceId = new InstanceId(type, id);
            
            if (m_instanceIds.TryGetValue(instanceId, out var provider)) {
                return provider.Instance;
            }

            if (m_parent != null) {
                return m_parent.Get(type, id);
            }

            throw new Exception($"Failed to get instance with id [{id}] for type [{type.FullName}]");
        }
        
        public void Inject(object instance)
        {
            var injectionTypeData = GetInjectionTypeData(instance.GetType());
            injectionTypeData.Inject(instance, this);
        }
        
        public void Clear(object instance)
        {
            var injectionTypeData = GetInjectionTypeData(instance.GetType());
            injectionTypeData.Clear(instance);
        }

        private static Injector GetInjectionTypeData(Type type)
        {
            if (!s_injectableTypes.TryGetValue(type, out var injectionTypeData)) {
                injectionTypeData = new Injector(type);
                s_injectableTypes.Add(type, injectionTypeData);
            }

            return injectionTypeData;
        }
    }
}
