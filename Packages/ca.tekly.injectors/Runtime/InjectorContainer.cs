using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.Logging;

namespace Tekly.Injectors
{
    public class InjectorContainer
    {
        private readonly Dictionary<Type, IInstanceProvider> m_instances = new Dictionary<Type, IInstanceProvider>();
        private readonly Dictionary<InstanceId, IInstanceProvider> m_instanceIds = new Dictionary<InstanceId, IInstanceProvider>();

        private readonly InjectorContainer m_parent;
        private readonly TkLogger m_logger = TkLogger.Get<InjectorContainer>();

        private List<ITypeInstanceProvider> m_typeProviders;

        public readonly string Name;
        
        public InjectorContainer(string name = null)
        {
            Name = name;
            Register(this);
        }
        
        public InjectorContainer(InjectorContainer parent, string name = null) : this(name)
        {
            m_parent = parent;
        }
        
        public void Singleton<T>()
        {
            m_instances.Add(typeof(T), SingletonProvider.Create<T>(this));
        }
        
        public void Singleton<TInterface, TImpl>() where TImpl : TInterface
        {
            m_instances.Add(typeof(TInterface), SingletonProvider.Create<TImpl>(this));
        }

        public void Map<TImpl, TInterface>() where TImpl : TInterface
        {
            var instance = m_instances[typeof(TImpl)];
            m_instances.Add(typeof(TInterface), instance);
        }
        
        public void SingletonWithInterfaces<T>()
        {
            var provider = SingletonProvider.Create<T>(this);

            var type = typeof(T);
            foreach (var interfaceType in type.GetInterfaces()) {
                if (interfaceType != typeof(IDisposable) && interfaceType != typeof(ITickable)) {
                    m_instances.Add(interfaceType, provider);
                }
            }

            m_instances.Add(type, provider);
        }
        
        public void Factory<T>()
        {
            m_instances.Add(typeof(T), FactoryProvider.Create<T>(this));
        }
        
        public void Register<T>(T instance)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", typeof(T)));
                return;
            }
            
            m_instances.Add(typeof(T), InstanceProvider.Create(instance));
        }
        
        public void Register<TInterface, TImpl>(TInterface instance) where TImpl : TInterface
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", typeof(TInterface)));
                return;
            }
            
            m_instances.Add(typeof(TInterface), InstanceProvider.Create(instance));
            m_instances.Add(typeof(TImpl), InstanceProvider.Create(instance));
        }
        
        public void Register(Type type, object instance)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", type));
                return;
            }
            
            m_instances.Add(type, InstanceProvider.Create(type, instance));
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
        
        public void Override<T>(T instance)
        {
            Override(typeof(T), instance);   
        }
        
        public void Override(Type type, object instance)
        {
            if (instance == null) {
                m_logger.Error("Trying to register instance of [{type}] but instance is null", ("type", type));
                return;
            }
            
            m_instances[type] = InstanceProvider.Create(type, instance);
        }

        public void RegisterTypeProvider(ITypeInstanceProvider instanceProvider)
        {
            if (m_typeProviders == null) {
                m_typeProviders = new List<ITypeInstanceProvider>();
            }
            
            m_typeProviders.Add(instanceProvider);
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
            if (TryGet(type, out var result)) {
                return result;
            }
            
            throw new Exception($"[{Name}] Failed to get instance for type [{type.FullName}]");
        }

        public virtual bool TryGet<T>(out T result)
        {
            if (TryGet(typeof(T), out var target)) {
                result = (T) target;
                return true;
            }

            result = default;
            return false;
        }
        
        public virtual bool TryGet(Type type, out object result)
        {
            if (m_instances.TryGetValue(type, out var provider)) {
                result = provider.Instance;
                return true;
            }

            if (TryGetFromTypeProviders(type, out result)) {
                return true;
            }

            if (m_parent != null && m_parent.TryGet(type, out result)) {
                return true;
            }

            result = default;
            return false;
        }
        
        public object Get(Type type, string id)
        {
            var instanceId = new InstanceId(type, id);
            
            if (TryGet(instanceId, out var result)) {
                return result;
            }
            
            throw new Exception($"[{Name}] Failed to get instance with id [{id}] for type [{type.FullName}]");
        }
        
        public bool TryGet(InstanceId instanceId, out object result)
        {
            if (m_instanceIds.TryGetValue(instanceId, out var provider)) {
                result = provider.Instance;
                return true;
            }
            
            if (m_parent != null && m_parent.TryGet(instanceId, out result)) {
                return true;
            }

            result = default;
            return false;
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
            return TypeDatabase.Instance.GetInjector(type);
        }

        private bool TryGetFromTypeProviders(Type type, out object result)
        {
            if (m_typeProviders == null) {
                result = default;
                return false;
            }

            for (var index = 0; index < m_typeProviders.Count; index++) {
                var typeProvider = m_typeProviders[index];
                if (typeProvider.CanProvide(type)) {
                    var provider = typeProvider.Provide(type);
                    m_instances[type] = provider;

                    result = provider.Instance;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}
