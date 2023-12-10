using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tekly.Common.Utils;
using Tekly.Content;
using Tekly.Logging;

namespace Tekly.Balance
{
    public class BalanceManager : IDisposable
    {
        public const string MANIFEST_ADDRESS = "balance_manifest";
        
        public bool IsInitialized { get; private set; }
        public string Version { get; set; }
        public bool IsLoading => !IsInitialized || m_banks.Values.Any(x => x.IsLoading);

        private readonly IContentProvider m_contentProvider;
        private readonly Dictionary<string, BalanceObject> m_balanceObjects = new Dictionary<string, BalanceObject>();
        private readonly TkLogger m_logger = TkLogger.Get<BalanceManager>();
        private readonly Dictionary<string, BalanceBank> m_banks = new Dictionary<string, BalanceBank>();
        
        private IContentOperation<BalanceManifest> m_balanceManifestHandle;
        private BalanceManifest m_balanceManifest;

        public BalanceManager(IContentProvider contentProvider)
        {
            m_contentProvider = contentProvider;
        }

        public async Task<Result> InitializeAsync()
        {
            try {
                if (m_balanceManifestHandle != null) {
                    m_logger.Error("Initializing BalanceManager more than once");
                    return Result.Okay();
                }
            
                m_balanceManifestHandle = m_contentProvider.LoadAssetAsync<BalanceManifest>(MANIFEST_ADDRESS);
                m_balanceManifest = await m_balanceManifestHandle;

                if (m_balanceManifest == null) {
                    m_logger.Error("Failed to find BalanceManifest [{name}]", ("name", MANIFEST_ADDRESS));
                    return Result.Fail("Failed to find BalanceManifest");
                } 
            
                IsInitialized = true;
                Version = m_balanceManifest.Version;
                TkLogger.SetCommonField("balanceVersion", m_balanceManifest.Version);
            
                m_logger.Info("BalanceManager initialized with version [{balanceVersion}]");
                return Result.Okay();
            } catch (Exception ex) {
                return Result.Fail("Exception initializing BalanceManager: " + ex.Message);
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

        public void LoadBank(string bankId)
        {
            if (m_banks.TryGetValue(bankId, out var bank)) {
                bank.AddRef();
            } else {
                bank = new BalanceBank(bankId, m_balanceObjects, ContentProvider.Instance);
                m_banks.Add(bankId, bank);
            } 
        }

        public void UnloadBank(string bankId)
        {
            if (!m_banks.TryGetValue(bankId, out var bank)) {
                return;
            }

            bank.RemoveRef();
            
            if (bank.References <= 0) {
                bank.Dispose();
                m_banks.Remove(bankId);
            }
        }
        
        public void Dispose()
        {
            m_balanceManifestHandle.Release();

            foreach (var bank in m_banks.Values) {
                bank.Dispose();
            }
            
            m_banks.Clear();
        }
    }
}