
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certify.Models;
using Certify.Models.Config;
using Certify.Models.Utils;

namespace Certify.Providers.DeploymentTasks.Core
{
    /// <summary>
    /// Provider Webhook deployment task. Webhook testing can be performed with `npx http-echo-server`
    /// </summary>
    public class Webhook : IDeploymentTaskProvider
    {
        public static DeploymentProviderDefinition Definition { get; }
        public DeploymentProviderDefinition GetDefinition(DeploymentProviderDefinition currentDefinition = null) => (currentDefinition ?? Definition);

        static Webhook()
        {
            Definition = new DeploymentProviderDefinition
            {
                Id = "Certify.Providers.DeploymentTasks.Webhook",
                Title = "Webhook",
                IsExperimental = false,
                Description = "Chamar um webhook personalizado em caso de sucesso ou falha na renovação",
                SupportedContexts = DeploymentContextType.LocalAsService,
                UsageType = DeploymentProviderUsage.Any,
                ProviderParameters = new System.Collections.Generic.List<ProviderParameter>
                {
                     new ProviderParameter{ Key="url", Name="Webhook URL", IsRequired=true, IsCredential=false , Description="A URL para a chamada webhook" },
                     new ProviderParameter{ Key="trigger", Name="Webhook Trigger", IsRequired=true, IsCredential=false , Description="O gatilho para o webhook (Nenhum, Successo, Erro)", OptionsList="None;Success;Error", Value="None" },
                     new ProviderParameter{ Key="method", Name="Método HTTP", IsRequired=true, IsCredential=false , Description="O método http para a chamada webhook", OptionsList="GET;POST;", Value="POST" },
                     new ProviderParameter{ Key="contenttype", Name="Tipo de conteúdo", IsRequired=true, IsCredential=false , Description="O tipo do conteúdo http para a chamada webhook", Value="application/json" },
                     new ProviderParameter{ Key="contentbody", Name="Corpo do conteúdo", IsRequired=true, IsCredential=false , Description="O modelo de corpo HTTP para a requisição do webhook" },
                }
            };
        }

        public async Task<List<ActionResult>> Execute(DeploymentTaskExecutionParams execParams)
        {
            var managedCert = ManagedCertificate.GetManagedCertificate(execParams.Subject);

            try
            {
                var webhookConfig = new WebhookConfig
                {
                    Url = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "url")?.Value,
                    Method = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "method")?.Value,
                    ContentType = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "contenttype")?.Value,
                    ContentBody = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "contentbody")?.Value
                };

                if (!execParams.IsPreviewOnly)
                {
                    var webHookResult = await SendRequest(webhookConfig, managedCert, managedCert?.LastRenewalStatus != RequestState.Error);

                    var msg = $"Webhook invoked: Url: {webhookConfig.Url}, Success: {webHookResult.Success}, StatusCode: {webHookResult.StatusCode}";

                    execParams.Log.Information(msg);

                    return new List<ActionResult> { new ActionResult(msg, true) };
                }
                else
                {
                    return await Validate(execParams);
                }
            }
            catch (Exception exp)
            {
                return new List<ActionResult> { new ActionResult("Chamada Webhook Falhou: " + exp.ToString(), false) };
            }
        }

        public async Task<List<ActionResult>> Validate(DeploymentTaskExecutionParams execParams)
        {
            var results = new List<ActionResult>();

            var url = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "url")?.Value;
            var method = execParams.Settings.Parameters.FirstOrDefault(p => p.Key == "method")?.Value;

            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var result))
            {
                results.Add(new ActionResult($"The webhook url must be a valid url.", false));
            }

            if (string.IsNullOrEmpty(method))
            {
                results.Add(new ActionResult($"O método HTTP do webhook deve ser selecionado.", false));
            }

            return await Task.FromResult(results);
        }

        /// <summary>
        /// Sends an HTTP Request with the requested parameters
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="contentType"></param>
        /// <param name="body"></param>
        /// <returns> A named Tuple with Success boolean and int StatusCode of the HTTP Request </returns>
        public static async Task<WebhookResult> SendRequest(WebhookConfig config, ManagedCertificate item, bool forSuccess)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Management.Util.GetUserAgent());

                HttpRequestMessage message;

                var url = ParseValues(config.Url, item?.RequestConfig, forSuccess, true);

                switch (config.Method)
                {
                    case Certify.Models.Utils.Webhook.METHOD_GET:
                        message = new HttpRequestMessage(HttpMethod.Get, url);
                        break;

                    case Certify.Models.Utils.Webhook.METHOD_POST:
                        message = new HttpRequestMessage(HttpMethod.Post, url)
                        {
                            Content = new StringContent(
                                ParseValues(!string.IsNullOrEmpty(config.ContentBody) ? config.ContentBody : Certify.Models.Utils.Webhook.DEFAULT_BODY, item.RequestConfig, forSuccess, false),
                                Encoding.UTF8,
                                string.IsNullOrEmpty(config.ContentType) ? "application/json" : config.ContentType
                                )
                        };
                        break;

                    default:
                        throw new ArgumentException("Método precisa ser GET ou POST", "method");
                }

                var resp = await client.SendAsync(message);

                return new WebhookResult(resp.IsSuccessStatusCode, (int)resp.StatusCode);
            }
        }

        /// <summary>
        /// Provides templating variable replacement for Config values
        /// </summary>
        /// <param name="template"></param>
        /// <param name="config"></param>
        /// <param name="forSuccess"></param>
        /// <returns></returns>
        private static string ParseValues(string template, CertRequestConfig config, bool forSuccess, bool url_encode)
        {
            // add all config properties to template vars
            var vars = new Dictionary<string, string>();

            foreach (var prop in config.GetType().GetProperties())
            {
                var objValue = prop.GetValue(config);

                var value = "";
                if (objValue != null && objValue is Array array)
                {
                    foreach (var i in array)
                    {
                        value += i.ToString() + " ";
                    }
                }
                else
                {
                    value = objValue?.ToString() ?? "";
                }

                if (url_encode)
                {
                    value = WebUtility.UrlEncode(value);
                }
                else
                {
                    value = value.Replace(@"\", @"\\");
                }

                vars[prop.Name.ToLower()] = value;
            }

            // ChallengeType can be multiple values, use the first one present

            vars["challengetype"] = config.Challenges.FirstOrDefault()?.ChallengeType ?? vars["challengetype"];

            // add special processing for these values
            vars["success"] = forSuccess ? "true" : "false";
            vars["subjectalternativenames"] = string.Join(",", config.SubjectAlternativeNames ?? new string[] { config.PrimaryDomain });

            // process the template and replace values
            return Regex.Replace(template, @"\$(\w+)(?=[\W$])", m =>
            {
                // replace var if it can be found, otherwise don't
                var key = m.Groups[1].Value.ToLower();
                return vars.ContainsKey(key) ? vars[key] : "$" + key;
            },
                RegexOptions.IgnoreCase);
        }
    }
}
