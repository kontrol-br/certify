using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certify.Models;
using Certify.Models.Providers;

namespace Certify.Management
{
    public class PreviewManager
    {
        /// <summary>
        /// Generate a list of actions which will be performed on the next renewal of this managed certificate, populating
        /// the description of each action with a Markdown format description
        /// </summary>
        /// <param name="item"></param>
        /// <param name="serverProvider"></param>
        /// <param name="certifyManager"></param>
        /// <returns></returns>
        public async Task<List<ActionStep>> GeneratePreview(
                    ManagedCertificate item,
                    ITargetWebServer serverProvider,
                    ICertifyManager certifyManager,
                    ICredentialsManager credentialsManager
            )
        {
            var newLine = "\r\n";
            var steps = new List<ActionStep>();

            var stepIndex = 1;

            var allTaskProviders = await certifyManager.GetDeploymentProviders();
            var certificateAuthorities = await certifyManager.GetCertificateAuthorities();

            // ensure defaults are applied for the deployment mode, overwriting any previous selections
            item.RequestConfig.ApplyDeploymentOptionDefaults();

            var identifiers = item.GetCertificateIdentifiers();

            if (identifiers.Any())
            {
                var allCredentials = await credentialsManager.GetCredentials();

                var allIdentifiers = item.GetCertificateIdentifiers();

                // certificate summary
                var certDescription = new StringBuilder();
                var ca = certificateAuthorities.FirstOrDefault(c => c.Id == item.CertificateAuthorityId);

                certDescription.AppendLine($"A new certificate will be requested from the **{ca?.Title.AsNullWhenBlank() ?? "Default"}** certificate authority for the following identifiers:");

                if (item.RequestConfig?.PreferredExpiryDays > 0)
                {
                    certDescription.AppendLine($"The certificate will be requested with a custom lifetime of **{item.RequestConfig.PreferredExpiryDays}** days.");
                }

                if (identifiers.Any(d => d.IdentifierType == CertIdentifierType.Dns))
                {

                    certDescription.AppendLine($"\n**{item.RequestConfig.PrimaryDomain}** (Primary Domain)");

                    if (item.RequestConfig.SubjectAlternativeNames.Any(s => s != item.RequestConfig.PrimaryDomain))
                    {
                        certDescription.AppendLine($" and will include the following *Subject Alternative Names*:");

                        foreach (var d in item.RequestConfig.SubjectAlternativeNames)
                        {
                            certDescription.AppendLine($"* {d} ");
                        }
                    }
                }

                if (identifiers.Any(d => d.IdentifierType != CertIdentifierType.Dns))
                {
                    foreach (var ident in identifiers.Where(i => i.IdentifierType != CertIdentifierType.Dns))
                    {
                        certDescription.AppendLine($"* {ident.Value} [{ident.IdentifierType}]");
                    }
                }

                steps.Add(new ActionStep
                {
                    Title = "Summary",
                    Description = certDescription.ToString()
                });

                // validation steps :
                // TODO: preview description should come from the challenge type provider

                var challengeInfo = new StringBuilder();
                foreach (var challengeConfig in item.RequestConfig.Challenges)
                {
                    challengeInfo.AppendLine(
                        $"{newLine}A autorização será tentada usando validação **{challengeConfig.ChallengeType}**." +
                        newLine
                        );

                    var matchingDomains = item.GetChallengeConfigDomainMatches(challengeConfig, allIdentifiers);
                    if (matchingDomains.Any())
                    {
                        challengeInfo.AppendLine(
                            $"{newLine}Os seguintes identificadores correspondentes usarão esta validação: " + newLine
                            );

                        foreach (var d in matchingDomains)
                        {
                            challengeInfo.AppendLine($"{newLine} * {d}");
                        }

                        if (allIdentifiers.Any(i => i.IdentifierType == CertIdentifierType.Dns))
                        {
                            challengeInfo.AppendLine(
                               $"{newLine}**Revise a seção de Implantação abaixo para garantir que este certificado será aplicado às associações de site esperadas (se houver).**" + newLine
                               );
                        }
                    }
                    else
                    {
                        challengeInfo.AppendLine(
                        $"{newLine}*Nenhum domínio corresponderá a este tipo de desafio. Ou o desafio não é necessário ou as correspondências de domínio não estão totalmente configuradas."
                        );
                    }

                    challengeInfo.AppendLine(newLine);

                    if (challengeConfig.ChallengeType == SupportedChallengeTypes.CHALLENGE_TYPE_HTTP)
                    {
                        challengeInfo.AppendLine(
                           $"Isso envolverá a criação de um arquivo de texto com nome aleatório (sem extensão) para cada domínio (site) incluído no certificado." +
                            newLine
                            );

                        if (CoreAppSettings.Current.EnableHttpChallengeServer)
                        {
                            challengeInfo.AppendLine(
                               $"A *Validação por HTTP* opç]ao está habilitada. Ist i´rá criar um serviço web temporário na porta 80 durante a validação. " +
                               $"Este processo irá coexistir com o seu web server principal e irá esperar pela conexão do desafio http apontando para /.well-known/acme-challenge/. " +
                               $"Se você está estiver usando a porta 80 em outro serviço web que não seja o IIS, ele será usado." +
                               newLine
                               );
                        }

                        if (!string.IsNullOrEmpty(item.RequestConfig.WebsiteRootPath) && string.IsNullOrEmpty(challengeConfig.ChallengeRootPath))
                        {
                            challengeInfo.AppendLine(
                                $"O arquivo será criado em `{item.RequestConfig.WebsiteRootPath}\\.well-known\\acme-challenge\\` " +
                                newLine
                                );
                        }

                        if (!string.IsNullOrEmpty(challengeConfig.ChallengeRootPath))
                        {
                            challengeInfo.AppendLine(
                                $"O arquivo será criado em `{challengeConfig.ChallengeRootPath}\\.well-known\\acme-challenge\\` " +
                                newLine
                                );
                        }

                        challengeInfo.AppendLine(
                            $"O arquivo texto que precisa ser acessado precisa estar disponível na URL `http://<yourdomain>/.well-known/acme-challenge/<randomfilename>` " +
                            newLine);

                        challengeInfo.AppendLine(
                            $"A Autoridade Certificadora emissora seguirá qualquer redirecionamento existente (como reescrever a URL para https), mas a solicitação inicial será feita via http na porta 80. " +
                            newLine);
                    }

                    if (challengeConfig.ChallengeType == SupportedChallengeTypes.CHALLENGE_TYPE_DNS)
                    {
                        challengeInfo.AppendLine(
                            $"Isso envolverá a criação de um registro TXT no DNS chamado _acme-challenge.seudominio.com para cada domínio ou subdomínio incluído no certificado. " +
                            newLine);

                        if (!string.IsNullOrEmpty(challengeConfig.ChallengeCredentialKey))
                        {
                            var creds = allCredentials.FirstOrDefault(c => c.StorageKey == challengeConfig.ChallengeCredentialKey);
                            if (creds != null)
                            {
                                challengeInfo.AppendLine(
                               $"A seguinte credencial DNS API será usada:  **{creds.Title}** " + newLine);
                            }
                            else
                            {
                                challengeInfo.AppendLine(
                                    $"**Configuração de credencial inválida.**  A credencial selecionada não existe."
                                    );
                            }
                        }
                        else
                        {
                            challengeInfo.AppendLine(
                                $"Nenhuma credencial de API DNS foi definida. As credenciais de API geralmente são necessárias para realizar atualizações automáticas nos registros DNS."
                                );
                        }

                        challengeInfo.AppendLine(
                            newLine + $"A Autoridade Certificadora emissora seguirá qualquer redirecionamento existente (como um CNAME substituto apontando para outro domínio), mas a solicitação inicial será feita contra qualquer um dos servidores de nomes do domínio. "
                            );
                    }

                    if (!string.IsNullOrEmpty(challengeConfig.DomainMatch))
                    {
                        challengeInfo.AppendLine(
                            $"{newLine}Este tipo de desafio será selecionado com base na correspondência de domínio **{challengeConfig.DomainMatch}** ");
                    }
                    else
                    {
                        if (item.RequestConfig.Challenges.Count > 1)
                        {
                            challengeInfo.AppendLine(
                             $"{newLine}Este tipo de validação será selecionada para qualquer identificador que não seja correspondido por outra validação. ");
                        }
                        else
                        {
                            challengeInfo.AppendLine(
                          $"{newLine}**esta validação será selecionada para todos os identificadores.**");
                        }
                    }
                }

                steps.Add(new ActionStep
                {
                    Title = $"{stepIndex}. Validation",
                    Category = "Validation",
                    Description = challengeInfo.ToString()
                });
                stepIndex++;

                // pre request tasks
                if (item.PreRequestTasks?.Any() == true)
                {
                    var substeps = item.PreRequestTasks.Select(t => new ActionStep { Key = t.Id, Title = $"{t.TaskName} ({allTaskProviders.FirstOrDefault(p => p.Id == t.TaskTypeId)?.Title})", Description = t.Description });

                    steps.Add(new ActionStep
                    {
                        Title = $"{stepIndex}. Pre-Request Tasks",
                        Category = "PreRequestTasks",
                        Description = $"Execute {substeps.Count()} Pre-Request Tasks",
                        Substeps = substeps.ToList()
                    });
                    stepIndex++;
                }

                // cert request step

                var preferredKeyType = !string.IsNullOrEmpty(item.RequestConfig.CSRKeyAlg) ? item.RequestConfig.CSRKeyAlg : CoreAppSettings.Current.DefaultKeyType;
                if (string.IsNullOrEmpty(preferredKeyType))
                {
                    preferredKeyType = StandardKeyTypes.RSA256; // MS Exchange etc do not support ECDSA key types so we default to RSA
                }

                var certRequest =
                    $"Uma Solicitação de Assinatura de Certificado (CSR) será enviada à Autoridade Certificadora, usando o algoritmo de assinatura **{preferredKeyType}**.";
                steps.Add(new ActionStep
                {
                    Title = $"{stepIndex}. Pedido de Certificado",
                    Category = "CertificateRequest",
                    Description = certRequest
                });
                stepIndex++;

                // deployment & binding steps

                var deploymentDescription = new StringBuilder();
                var deploymentStep = new ActionStep
                {
                    Title = $"{stepIndex}. Implantação",
                    Category = "Deployment",
                    Description = ""
                };

                if (
                    item.RequestConfig.DeploymentSiteOption == DeploymentOption.Auto ||
                    item.RequestConfig.DeploymentSiteOption == DeploymentOption.AllSites ||
                    item.RequestConfig.DeploymentSiteOption == DeploymentOption.SingleSite
                )
                {
                    // deploying to single or multiple Site

                    if (serverProvider == null)
                    {
                        deploymentDescription.AppendLine($"* A instância de destino não possui alvos compatíveis para implantação automática (por exemplo, IIS). Tarefas de implantação ainda podem ser aplicadas.");
                    }
                    else
                    {

                        if (item.RequestConfig.DeploymentBindingMatchHostname)
                        {
                            deploymentDescription.AppendLine(
                                "* Implantar nas associações de nome de host que correspondem aos domínios do certificado.");
                        }

                        if (item.RequestConfig.DeploymentBindingBlankHostname)
                        {
                            deploymentDescription.AppendLine("* Implantar em associações com nome de host em branco.");
                        }

                        if (item.RequestConfig.DeploymentBindingReplacePrevious)
                        {
                            deploymentDescription.AppendLine("* Implantar nas associações que utilizam o certificado anterior.");
                        }

                        if (item.RequestConfig.DeploymentBindingOption == DeploymentBindingOption.AddOrUpdate)
                        {
                            deploymentDescription.AppendLine("* Adicionar ou atualizar associações HTTPS conforme necessário");
                        }

                        if (item.RequestConfig.DeploymentBindingOption == DeploymentBindingOption.UpdateOnly)
                        {
                            deploymentDescription.AppendLine("* Atualizar associações HTTPS conforme necessário (sem criação automática de novas associações HTTPS)");
                        }

                        if (item.RequestConfig.DeploymentSiteOption == DeploymentOption.SingleSite)
                        {
                            if (!string.IsNullOrEmpty(item.ServerSiteId))
                            {
                                try
                                {
                                    var siteInfo = await serverProvider.GetSiteById(item.ServerSiteId);
                                    deploymentDescription.AppendLine($"## Deploying to Site" + newLine + newLine +
                                                             $"`{siteInfo.Name}`" + newLine);
                                }
                                catch (Exception exp)
                                {
                                    deploymentDescription.AppendLine($"Error: **cannot identify selected site.** {exp.Message} ");
                                }
                            }
                        }
                        else
                        {
                            deploymentDescription.AppendLine($"## Deploying to all matching sites:");
                        }
                    }

                    // add deployment sub-steps (if any)
                    var bindingRequest = await certifyManager.DeployCertificate(item, null, true);
                    if (bindingRequest.Actions?.Any(b => b.Category == "CertificateStorage") == true)
                    {
                        var defaultValue = "Unknown Storage";
                        deploymentDescription.AppendLine(bindingRequest.Actions.First(b => b.Category == "CertificateStorage")?.Description.WithDefault(defaultValue) ?? defaultValue);
                    }
                    else
                    {
                        deploymentDescription.AppendLine("**Certificado não será adicionado na pasta de certificados da máquina**");

                        deploymentStep.Substeps = new List<ActionStep>
                        {
                            new ActionStep {Description = newLine + "**Certificado não será adicionado na pasta de certificados da máquina**"}
                        };
                    }

                    if (!bindingRequest.Actions?.Any(b => b.Category.StartsWith("Deployment")) == true)
                    {
                        deploymentStep.Substeps = new List<ActionStep>
                        {
                            new ActionStep {Description = newLine + "**Não há destinos correspondentes para implantar. O certificado será armazenado, mas atualmente nenhuma associação será atualizada.**"}
                        };
                    }
                    else
                    {
                        deploymentStep.Substeps = bindingRequest.Actions.Where(b => b.Category.StartsWith("Deployment")).ToList();

                        deploymentDescription.AppendLine("\n Action | Site | Binding ");
                        deploymentDescription.Append(" ------ | ---- | ------- ");
                    }
                }
                else if (item.RequestConfig.DeploymentSiteOption == DeploymentOption.DeploymentStoreOnly)
                {
                    deploymentDescription.AppendLine("* O certificado será salvo apenas na pasta de Certificados da máquina local (Certificados Pessoais/Meus certificados");
                }
                else if (item.RequestConfig.DeploymentSiteOption == DeploymentOption.NoDeployment)
                {
                    deploymentDescription.AppendLine("* O certificado será guardado no disco local.");
                }

                deploymentStep.Description = deploymentDescription.ToString();

                steps.Add(deploymentStep);
                stepIndex++;

                // post request deployment tasks
                if (item.PostRequestTasks?.Any() == true)
                {

                    var substeps = item.PostRequestTasks.Select(t => new ActionStep { Key = t.Id, Title = $"{t.TaskName} ({allTaskProviders.FirstOrDefault(p => p.Id == t.TaskTypeId)?.Title})", Description = t.Description });

                    steps.Add(new ActionStep
                    {
                        Title = $"{stepIndex}. Post-Request (Deployment) Tasks",
                        Category = "PostRequestTasks",
                        Description = $"Execute {substeps.Count()} Post-Request Tasks",
                        Substeps = substeps.ToList()
                    });
                    stepIndex++;
                }

                stepIndex = steps.Count;
            }
            else
            {
                steps.Add(new ActionStep
                {
                    Title = "Certificado não possui domínios",
                    Description = "Nenhum domínio foi adicionado a este certificado, portanto, um certificado não pode ser solicitado. Cada certificado requer um domínio principal (um 'Subject') e uma lista opcional de domínios adicionais (Subject Alternative Names)."
                });
            }

            return steps;
        }

        /// <summary>
        /// WIP: For current configured environment, show preview of recommended site management (for
        ///      local IIS, scan sites and recommend actions)
        /// </summary>
        /// <returns></returns>
        public async Task<List<ManagedCertificate>> PreviewManagedCertificates(StandardServerTypes serverType,
            ITargetWebServer serverProvider, ICertifyManager certifyManager)
        {
            var sites = new List<ManagedCertificate>();

            try
            {
                var allSites = await serverProvider.GetSiteBindingList(CoreAppSettings.Current.IgnoreStoppedSites);
                var targetSites = allSites
                    .OrderBy(s => s.SiteId)
                    .ThenBy(s => s.Host);

                var siteIds = targetSites.GroupBy(x => x.SiteId);

                foreach (var s in siteIds)
                {
                    var managedCertificate = new ManagedCertificate { Id = s.Key };
                    managedCertificate.ItemType = ManagedCertificateType.SSL_ACME;

                    managedCertificate.Name = targetSites.First(i => i.SiteId == s.Key).SiteName;

                    sites.Add(managedCertificate);
                }
            }
            catch (Exception)
            {
                //can't read sites
                Debug.WriteLine("Não pude listar os servidores de destino.");
            }

            return sites;
        }
    }
}
