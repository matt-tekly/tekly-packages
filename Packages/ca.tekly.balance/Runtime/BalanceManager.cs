using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tekly.Content;
using Tekly.Logging;

namespace Tekly.Balance
{
    public class BalanceManager : IDisposable
    {
        public const string MANIFEST_ADDRESS = "balance_manifest";
        
        public bool IsInitialized { get; private set; }
        public string Version { get; set; }

        private readonly IContentProvider m_contentProvider;
        private readonly Dictionary<string, BalanceObject> m_balanceObjects = new Dictionary<string, BalanceObject>();
        private readonly TkLogger m_logger = TkLogger.Get<BalanceManager>();
        
        private IContentOperation<BalanceManifest> m_balanceManifestHandle;
        private BalanceManifest m_balanceManifest;

        public BalanceManager(IContentProvider contentProvider)
        {
            m_contentProvider = contentProvider;
        }

        public async Task InitializeAsync()
        {
            if (m_balanceManifestHandle != null) {
                m_logger.Error("Initializing BalanceManager more than once");
                return;
            }
            
            m_balanceManifestHandle = m_contentProvider.LoadAssetAsync<BalanceManifest>(MANIFEST_ADDRESS);
            m_balanceManifest = await m_balanceManifestHandle;

            if (m_balanceManifest != null) {
                IsInitialized = true;
                Version = m_balanceManifest.Version;
                TkLogger.SetValue("balanceVersion", m_balanceManifest.Version);
                m_logger.Info("BalanceManager initialized with version [{balanceVersion}]");
            } else {
                m_logger.Error("Failed to find BalanceManifest [{name}]", ("name", MANIFEST_ADDRESS));
            }
        }
        
        public T Get<T>(string name) where T : BalanceObject
        {
            return m_balanceObjects[name] as T;
        }
        
        public bool TryGet<T>(string name, out T result) where T : BalanceObject
        {
            if (m_balanceObjects.TryGetValue(name, out var value)) {
                result = value as T;
                if (result == null) {
                    m_logger.Error("Found BalanceObject [{name}] but was the wrong type", ("name", name));
                    return false;
                }

                return true;
            }

            result = null;
            return false;
        }

        public List<T> GetAll<T>() where T : BalanceObject
        {
            return m_balanceObjects.Values.OfType<T>().ToList();
        }
        
        public void AddContainer(BalanceContainer balanceContainer)
        {
            foreach (var balanceObject in balanceContainer.Objects) {
                m_balanceObjects[balanceObject.name] = balanceObject;
            }
        }

        public void RemoveContainer(BalanceContainer balanceContainer)
        {
            foreach (var balanceObject in balanceContainer.Objects) {
                m_balanceObjects.Remove(balanceObject.name);
            }
        }

        public void Dispose()
        {
            m_balanceManifestHandle.Release();
        }
    }
}