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
        private const string DEFAULT_API_ENDPOINT = "https://update.autoip.com.br/acme";

        private HttpClient _http;
        private ILog _log;
        private string _credential; // username or token
        private string _password;
        private string _hostname;
        private string _apiEndpoint = DEFAULT_API_ENDPOINT;
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
                new ProviderParameter{ Key="apiendpoint", Name="API Endpoint", IsRequired=false, IsCredential=false, IsPassword=false, Value=DEFAULT_API_ENDPOINT },
                new ProviderParameter{ Key="username", Name="Username/Token Acme", IsRequired=false, IsCredential=true },
                new ProviderParameter{ Key="password", Name="Password", IsRequired=false, IsCredential=true, IsPassword=true },
                new ProviderParameter{ Key="hostname", Name="Hostname", IsRequired=false },
                new ProviderParameter{ Key="propagationdelay", Name="Tempo de Propagação em Segundos", IsRequired=false, IsCredential=false, IsPassword=false, Value="60" }
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
            credentials?.TryGetValue("username", out _credential);
            if (string.IsNullOrEmpty(_credential))
            {
                credentials?.TryGetValue("acmetoken", out _credential);
            }
            credentials?.TryGetValue("password", out _password);
            parameters?.TryGetValue("hostname", out _hostname);
            parameters?.TryGetValue("apiendpoint", out _apiEndpoint);

            if (parameters?.ContainsKey("propagationdelay") == true && int.TryParse(parameters["propagationdelay"], out var delay))
            {
                _customPropagationDelay = delay;
            }

            _http = new HttpClient();
            if (!string.IsNullOrEmpty(_credential))
            {
                if (string.IsNullOrEmpty(_password))
                {
                    _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _credential);
                }
                else
                {
                    var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credential}:{_password}"));
                    _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
                }
            }
            var authType = string.IsNullOrEmpty(_password) ? "token" : "user";
            var passwordDisplay = string.IsNullOrEmpty(_password) ? string.Empty : "****";
            _log?.Information("AutoIP provider initialized. Endpoint: {Endpoint}. AuthType: {AuthType}. Credential: {Credential} {Password}",
                _apiEndpoint, authType, _credential, passwordDisplay);

            return Task.FromResult(true);
        }

        public async Task<ActionResult> CreateRecord(DnsRecord request)
        {
            try
            {
                var payload = new Dictionary<string, string>{
                    { "txt", request.RecordValue }
                };

                if (!string.IsNullOrEmpty(_hostname))
                {
                    payload.Add("hostname", _hostname);
                }

                var json = JsonConvert.SerializeObject(payload);
                var authInfo = string.IsNullOrEmpty(_password) ? $"token {_credential}" : $"user {_credential} / ****";
                _log?.Information("HTTP POST {Url} Payload: {Payload}. Auth: {AuthInfo}", _apiEndpoint, json, authInfo);
                var resp = await _http.PostAsync(_apiEndpoint, new StringContent(json, Encoding.UTF8, "application/json"));

                if (resp.IsSuccessStatusCode)
                {
                    return new ActionResult { IsSuccess = true, Message = "DNS record added." };
                }

                var error = await resp.Content.ReadAsStringAsync();
                var message = $"API call failed: {resp.StatusCode} - {error}";
                _log?.Error(message);
                return new ActionResult { IsSuccess = false, Message = message };
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
                if (!string.IsNullOrEmpty(_hostname))
                {
                    payload.Add("hostname", _hostname);
                }

                var json = payload.Count > 0 ? JsonConvert.SerializeObject(payload) : string.Empty;
                var authInfo = string.IsNullOrEmpty(_password) ? $"token {_credential}" : $"user {_credential} / ****";
                _log?.Information("HTTP DELETE {Url} Payload: {Payload}. Auth: {AuthInfo}", _apiEndpoint, json, authInfo);
                var req = new HttpRequestMessage(HttpMethod.Delete, _apiEndpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                var resp = await _http.SendAsync(req);

                if (resp.IsSuccessStatusCode)
                {
                    return new ActionResult { IsSuccess = true, Message = "DNS record removed." };
                }

                var error = await resp.Content.ReadAsStringAsync();
                var message = $"API call failed: {resp.StatusCode} - {error}";
                _log?.Error(message);
                return new ActionResult { IsSuccess = false, Message = message };
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

        public override Task<List<DnsZone>> GetZones()
        {
            // AutoIP API does not provide a way to enumerate zones, so return an empty list.
            return Task.FromResult(new List<DnsZone>());
        }
    }
}
