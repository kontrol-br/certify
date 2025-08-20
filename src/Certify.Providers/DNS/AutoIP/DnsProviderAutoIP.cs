using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Certify.Models.Config;
using Certify.Models.Plugins;
using Certify.Models.Providers;
using Certify.Plugins;
using Newtonsoft.Json;

namespace Certify.Providers.DNS.AutoIP
{
    public class DnsProviderAutoIPProvider : PluginProviderBase<IDnsProvider, ChallengeProviderDefinition>, IDnsProviderProviderPlugin { }

    public class DnsProviderAutoIP : DnsProviderBase, IDnsProvider
    {
        private const string API_ENDPOINT = "https://update.autoip.com.br/acme";

        private HttpClient _http;
        private ILog _log;
        private string _token;
        private string _username;
        private string _password;
        private string _hostname;
        private int? _customPropagationDelay = null;

        public DnsProviderAutoIP()
        {
        }

        public static ChallengeProviderDefinition Definition => new ChallengeProviderDefinition
        {
            Id = "DNS01.API.AutoIP",
            Title = "AutoIP DNS API",
            Description = "Validates via AutoIP DNS API using token or user credentials.",
            HelpUrl = "https://update.autoip.com.br/",
            PropagationDelaySeconds = 60,
            ProviderParameters = new List<ProviderParameter>{
                new ProviderParameter{ Key="apitoken", Name="API Token", IsRequired=false, IsCredential=true, IsPassword=true },
                new ProviderParameter{ Key="username", Name="User Name", IsRequired=false, IsCredential=true },
                new ProviderParameter{ Key="password", Name="Password", IsRequired=false, IsCredential=true, IsPassword=true },
                new ProviderParameter{ Key="hostname", Name="Hostname", IsRequired=false },
                new ProviderParameter{ Key="propagationdelay", Name="Propagation Delay Seconds", IsRequired=false, IsCredential=false, IsPassword=false, Value="60" }
            },
            ChallengeType = Models.SupportedChallengeTypes.CHALLENGE_TYPE_DNS,
            Config = "Provider=Certify.Providers.DNS.AutoIP",
            HandlerType = ChallengeHandlerType.INTERNAL
        };

        public int PropagationDelaySeconds => (_customPropagationDelay != null ? (int)_customPropagationDelay : Definition.PropagationDelaySeconds);

        public string ProviderId => Definition.Id;
        public string ProviderTitle => Definition.Title;
        public string ProviderDescription => Definition.Description;
        public string ProviderHelpUrl => Definition.HelpUrl;
        public bool IsTestModeSupported => Definition.IsTestModeSupported;
        public List<ProviderParameter> ProviderParameters => Definition.ProviderParameters;

        public Task<bool> InitProvider(Dictionary<string, string> credentials, Dictionary<string, string> parameters, ILog log = null)
        {
            _log = log;
            credentials?.TryGetValue("apitoken", out _token);
            credentials?.TryGetValue("username", out _username);
            credentials?.TryGetValue("password", out _password);
            parameters?.TryGetValue("hostname", out _hostname);

            if (parameters?.ContainsKey("propagationdelay") == true && int.TryParse(parameters["propagationdelay"], out var delay))
            {
                _customPropagationDelay = delay;
            }

            _http = new HttpClient();
            if (!string.IsNullOrEmpty(_token))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
            }
            else if (!string.IsNullOrEmpty(_username))
            {
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            }

            return Task.FromResult(true);
        }

        public async Task<ActionResult> CreateRecord(DnsRecord request)
        {
            try
            {
                var payload = new Dictionary<string, string>{
                    { "txt", request.RecordValue }
                };

                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_hostname))
                {
                    payload.Add("hostname", _hostname);
                }

                var json = JsonConvert.SerializeObject(payload);
                var resp = await _http.PostAsync(API_ENDPOINT, new StringContent(json, Encoding.UTF8, "application/json"));

                if (resp.IsSuccessStatusCode)
                {
                    return new ActionResult { IsSuccess = true, Message = "DNS record added." };
                }

                return new ActionResult { IsSuccess = false, Message = $"API call failed: {resp.StatusCode}" };
            }
            catch (Exception ex)
            {
                return new ActionResult { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ActionResult> DeleteRecord(DnsRecord request)
        {
            try
            {
                var payload = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_hostname))
                {
                    payload.Add("hostname", _hostname);
                }

                var json = JsonConvert.SerializeObject(payload);
                var req = new HttpRequestMessage(HttpMethod.Delete, API_ENDPOINT)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var resp = await _http.SendAsync(req);

                if (resp.IsSuccessStatusCode)
                {
                    return new ActionResult { IsSuccess = true, Message = "DNS record removed." };
                }

                return new ActionResult { IsSuccess = false, Message = $"API call failed: {resp.StatusCode}" };
            }
            catch (Exception ex)
            {
                return new ActionResult { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<ActionResult> Test()
        {
            try
            {
                // Attempt to create and remove a test record
                var testValue = Guid.NewGuid().ToString("n");
                var rec = new DnsRecord { RecordName = "_acme-challenge.test", RecordValue = testValue };
                var add = await CreateRecord(rec);
                if (!add.IsSuccess)
                {
                    return add;
                }

                var del = await DeleteRecord(rec);
                if (!del.IsSuccess)
                {
                    return del;
                }

                return new ActionResult { IsSuccess = true, Message = "API Test Completed OK." };
            }
            catch (Exception ex)
            {
                return new ActionResult { IsSuccess = false, Message = $"API Test Failed: {ex.Message}" };
            }
        }

        public Task<List<DnsZone>> GetZones()
        {
            return Task.FromResult(new List<DnsZone>());
        }
    }
}
