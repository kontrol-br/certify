using System.Collections.Generic;
using Certify.Models;

namespace Certify.CertificateAuthorities.Definitions
{

    internal sealed class LetsEncrypt
    {
        public static CertificateAuthority GetDefinition()
        {
            return new CertificateAuthority
            {
                Id = "letsencrypt.org",
                Title = "Let's Encrypt",
                Description = "Let's Encrypt É uma autoridade certificadora gratuita, automatizada e aberta. Os certificados são válidos por 90 dias e podem conter até 100 domínios/subdomínios ou curingas.",
                APIType = CertAuthorityAPIType.ACME_V2.ToString(),
                WebsiteUrl = "https://letsencrypt.org/",
                PrivacyPolicyUrl = "https://letsencrypt.org/privacy/",
                TermsAndConditionsUrl = "https://letsencrypt.org/repository/",
                ProductionAPIEndpoint = "https://acme-v02.api.letsencrypt.org/directory",
                StagingAPIEndpoint = "https://acme-staging-v02.api.letsencrypt.org/directory",
                StatusUrl = "https://letsencrypt.status.io/",
                IsEnabled = true,
                IsCustom = false,
                SANLimit = 100,
                StandardExpiryDays = 90,
                RequiresEmailAddress = false,
                SupportsCachedValidations = true,
                SupportedFeatures = new List<string>{
                    CertAuthoritySupportedRequests.DOMAIN_SINGLE.ToString(),
                    CertAuthoritySupportedRequests.DOMAIN_MULTIPLE_SAN.ToString(),
                    CertAuthoritySupportedRequests.DOMAIN_WILDCARD.ToString()
                },
                SupportedKeyTypes = new List<string>{
                    StandardKeyTypes.RSA256,
                    StandardKeyTypes.RSA256_3072,
                    StandardKeyTypes.RSA256_4096,
                    StandardKeyTypes.ECDSA256,
                    StandardKeyTypes.ECDSA384
                }
            };
        }

        public static List<ChainOption> GetChainOptions()
        {
            return new List<ChainOption>
            {
                new ChainOption {
                    Id="letsencrypt-rsa-modern",
                    Name="Modern Chain (ISRG Root X1)",
                    Issuer="ISRG Root X1",
                    ChainGroup="RSA",
                    Description="Mude para esta cadeia para servir a cadeia mais curta em sistemas operacionais modernos que confiam na ISRG Root X1.",
                    Actions= new List<ChainAction>
                    {
                        new ChainAction (ChainActions.Delete, "933c6ddee95c9c41a40f9f50493d82be03ad87bf", "Remover o ISRG Root X1 assinado cruzadamente pelo DST Root CA X3"),
                        new ChainAction (ChainActions.StoreCARoot, "cabd2a79a1076a31f21d253635cb039d4329a5e8", "Adicionar o ISRG Root X1 autoassinado")
                    }
                },
                new ChainOption
                {
                    Id = "letsencrypt-rsa-legacy",
                    Name = "Legacy Chain (DST Root CA X3)",
                    Issuer = "DST Root CA X3",
                    ChainGroup = "RSA",
                    Description = "Mude para esta cadeia para servir a cadeia mais longa (mais compatível), a fim de oferecer suporte a sistemas operacionais que não confiam na ISRG Root X1.",
                    Actions = new List<ChainAction>
                    {
                        new ChainAction (ChainActions.StoreCAIntermediate, "933c6ddee95c9c41a40f9f50493d82be03ad87bf", "Adicionar o ISRG Root X1 assinado cruzadamente pelo DST Root CA X3")
                    }
                }
            };
        }
    }
}
