using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Certify.Core.Management.Challenges;
using Certify.Management;
using Certify.Models;
using Certify.Models.Config;
using Certify.Models.Providers;
using Certify.Models.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Certify.Core.Tests.Unit
{
    [TestClass]
    public class DnsCleanupTests
    {
        private const string MockProviderId = "DNS01.Mock";

        class MockDnsProvider : IDnsProvider
        {
            public static int DeleteCount = 0;
            public Task<bool> InitProvider(Dictionary<string, string> credentials, Dictionary<string, string> parameters, ILog log = null) => Task.FromResult(true);
            public Task<ActionResult> Test() => Task.FromResult(new ActionResult { IsSuccess = true });
            public Task<ActionResult> CreateRecord(DnsRecord request) => Task.FromResult(new ActionResult { IsSuccess = true });
            public Task<ActionResult> DeleteRecord(DnsRecord request)
            {
                DeleteCount++;
                return Task.FromResult(new ActionResult { IsSuccess = true });
            }
            public Task<List<DnsZone>> GetZones() => Task.FromResult(new List<DnsZone>());
            public int PropagationDelaySeconds => 0;
            public string ProviderId => MockProviderId;
            public string ProviderTitle => "Mock DNS";
            public string ProviderDescription => "";
            public string ProviderHelpUrl => "";
            public bool IsTestModeSupported => true;
            public List<ProviderParameter> ProviderParameters => new List<ProviderParameter>();
        }

        class MockDnsProviderPlugin : IDnsProviderProviderPlugin
        {
            public IDnsProvider GetProvider(Type pluginType, string id) => id == MockProviderId ? new MockDnsProvider() : null;
            public IEnumerable<ChallengeProviderDefinition> GetProviders(Type pluginType) => new List<ChallengeProviderDefinition>
            {
                new ChallengeProviderDefinition
                {
                    Id = MockProviderId,
                    Title = "Mock DNS",
                    Description = "Mock",
                    ChallengeType = SupportedChallengeTypes.CHALLENGE_TYPE_DNS,
                    HandlerType = ChallengeHandlerType.INTERNAL,
                    ProviderParameters = new List<ProviderParameter>()
                }
            };
        }

        [TestMethod]
        public async Task DeletesQueuedUntilFlush()
        {
            var pluginManager = PluginManager.CurrentInstance ?? new PluginManager();
            pluginManager.DnsProviderProviders ??= new List<IDnsProviderProviderPlugin>();
            pluginManager.DnsProviderProviders.Add(new MockDnsProviderPlugin());

            var credentialsManager = new Certify.Datastore.SQLite.SQLiteCredentialStore("Tests\\credentials");
            var dnsHelper = new DnsChallengeHelper(credentialsManager);

            CoreAppSettings.Current.ChallengeCleanupMode = ChallengeCleanupMode.PostValidation;

            var mc = new ManagedCertificate
            {
                RequestConfig = new CertRequestConfig
                {
                    Challenges = new ObservableCollection<CertRequestChallengeConfig>
                    {
                        new CertRequestChallengeConfig
                        {
                            ChallengeType = SupportedChallengeTypes.CHALLENGE_TYPE_DNS,
                            ChallengeProvider = MockProviderId
                        }
                    }
                }
            };

            var log = new Models.Loggy(null);

            var d1 = new CertIdentifierItem { IdentifierType = CertIdentifierType.Dns, Value = "one.example.com" };
            var d2 = new CertIdentifierItem { IdentifierType = CertIdentifierType.Dns, Value = "two.example.com" };

            await dnsHelper.DeleteDNSChallenge(log, mc, d1, "_acme-challenge.one.example.com", "value1");
            await dnsHelper.DeleteDNSChallenge(log, mc, d2, "_acme-challenge.two.example.com", "value2");

            Assert.AreEqual(0, MockDnsProvider.DeleteCount);

            await DnsChallengeHelper.ProcessPendingDeletes(log);

            Assert.AreEqual(2, MockDnsProvider.DeleteCount);
        }
    }
}
