using System;
using System.Collections.Generic;
using System.Linq;
using Certify.Models;
using Certify.Models.Hub;
using SourceGenerator;

namespace Certify.SourceGenerators
{
    // ApiMethods is a static class that provides a central registry of API endpoint definitions.
    //
    // Purpose:
    //   - Used by source generators to automatically create API endpoints, map calls between public and internal APIs, and generate API clients.
    //
    // Key Features:
    //   - Defines HTTP method constants (HttpGet, HttpPost, HttpDelete) for standardized usage.
    //   - Provides GetFormattedTypeName(Type type) to return a formatted string for .NET types, including generics.
    //   - The GetApiDefinitions() method returns a list of GeneratedAPI objects, each describing an API operation:
    //       * OperationName: Name of the API operation.
    //       * OperationMethod: HTTP method (GET, POST, DELETE).
    //       * Comment: Description of the operation.
    //       * PublicAPIController/PublicAPIRoute: Controller and route for the public API.
    //       * ServiceAPIRoute: Route for the internal service API.
    //       * ReturnType: The return type of the operation.
    //       * Params: Dictionary of parameter names and types.
    //       * RequiredPermissions: List of permissions required to access the endpoint.
    //       * Other flags like UseManagementAPI for special routing.
    //
    // Usage:
    //   - This list is consumed by source generators to:
    //       * Generate controller methods for the public API.
    //       * Map public API calls to internal service methods.
    //       * Generate client code for consuming the API.
    //
    // Summary:
    //   ApiMethods enables automated code generation for API endpoints, permissions, and client libraries in the Certify system, reducing manual coding and ensuring consistency across the API surface.
    public class ApiMethods
    {
        public static string HttpGet = "HttpGet";
        public static string HttpPost = "HttpPost";
        public static string HttpDelete = "HttpDelete";

        public static string GetFormattedTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments()
                                    .Select(x => x.FullName)
                                    .Aggregate((x1, x2) => $"{x1}, {x2}");
                return $"{type.FullName.Substring(0, type.FullName.IndexOf("`"))}"
                     + $"<{genericArguments}>";
            }

            return type.FullName;
        }

        public static List<GeneratedAPI> GetApiDefinitions()
        {
            // declaring an API definition here is then used by the source generators to:
            // - create the public API endpoint
            // - map the call from the public API to the background service API in the service API Client (interface and implementation)
            // - to then generate the public API clients, run nswag when the public API is running.

            var actionResultTypeName = "Certify.Models.Config.ActionResult";

            return new List<GeneratedAPI>
            {

                new()
                {
                    OperationName = "CheckSecurityPrincipalHasAccess",
                    OperationMethod = HttpPost,
                    Comment = "Verificar se uma determinada entidade de segurança possui permissões para executar uma ação específica para um recurso específico",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal/allowedaction",
                    ServiceAPIRoute = "access/securityprincipal/allowedaction",
                    ReturnType = "bool",
                    Params = new Dictionary<string, string> { { "check", nameof(Certify.Models.Hub.AccessCheck) } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalCheckAccess)]
                },
                new()
                {
                    OperationName = "GetSecurityPrincipalAssignedRoles",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de Funções Atribuídas para uma determinada entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal/{id}/assignedroles",
                    ServiceAPIRoute = "access/securityprincipal/{id}/assignedroles",
                    ReturnType = $"ICollection<{nameof(AssignedRole)}>",
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalCheckAccess)]
                },
                new()
                {
                    OperationName = "GetSecurityPrincipalRoleStatus",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de Funções Atribuídas etc para uma determinada entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal/{id}/rolestatus",
                    ServiceAPIRoute = "access/securityprincipal/{id}/rolestatus",
                    ReturnType = nameof(RoleStatus),
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalCheckAccess)]
                },
                new()
                {
                    OperationName = "GetAccessRoles",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de Funções de segurança disponíveis",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "roles",
                    ServiceAPIRoute = "access/roles",
                    ReturnType = $"ICollection<{nameof(Role)}>",
                    RequiredPermissions = [new(ResourceTypes.Role, StandardResourceActions.RoleList)]
                },
                new()
                {
                    OperationName = "GetAssignedAccessTokens",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de tokens de acesso atribuídos da API",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "assignedtoken",
                    ServiceAPIRoute = "access/assignedtoken/list",
                    ReturnType = $"ICollection<{nameof(AssignedAccessToken)}>",
                    RequiredPermissions = [new(ResourceTypes.AccessToken, StandardResourceActions.AccessTokenList)]
                },
                new()
                {
                    OperationName = "AddAssignedAccessToken",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar novo token de acesso atribuído",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "assignedtoken",
                    ServiceAPIRoute = "access/assignedtoken",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "token", "Certify.Models.Hub.AssignedAccessToken" } },
                    RequiredPermissions = [new(ResourceTypes.AccessToken, StandardResourceActions.AccessTokenAdd)]
                },
                new()
                {
                    OperationName = "RemoveAssignedAccessToken",
                    OperationMethod = HttpDelete,
                    Comment = "Remover token de acesso atribuído",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "assignedtoken",
                    ServiceAPIRoute = "access/assignedtoken/{id}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.AccessToken, StandardResourceActions.AccessTokenDelete)]
                },
                new()
                {

                    OperationName = "GetSecurityPrincipals",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de entidades de segurança disponíveis",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipals",
                    ServiceAPIRoute = "access/securityprincipals",
                    ReturnType = "ICollection<SecurityPrincipal>",
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalList)]
                },
                new()
                {
                    OperationName = "ValidateSecurityPrincipalPassword",
                    OperationMethod = HttpPost,
                    Comment = "Verificar se a senha é válida para a entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "validate",
                    ServiceAPIRoute = "access/validate",
                    ReturnType = "Certify.Models.Hub.SecurityPrincipalCheckResponse",
                    Params = new Dictionary<string, string> { { "passwordCheck", GetFormattedTypeName(typeof(Certify.Models.Hub.SecurityPrincipalPasswordCheck)) } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalPasswordValidate)]
                },
                new()
                {

                    OperationName = "UpdateSecurityPrincipalPassword",
                    OperationMethod = HttpPost,
                    Comment = "Atualizar senha para a entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "updatepassword",
                    ServiceAPIRoute = "access/updatepassword",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "passwordUpdate", "Certify.Models.Hub.SecurityPrincipalPasswordUpdate" } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalPasswordUpdate)]
                },
                new()
                {

                    OperationName = "AddSecurityPrincipal",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar nova entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal",
                    ServiceAPIRoute = "access/securityprincipal",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "principal", "Certify.Models.Hub.SecurityPrincipal" } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalAdd)]
                },
                new()
                {

                    OperationName = "UpdateSecurityPrincipal",
                    OperationMethod = HttpPost,
                    Comment = "Atualizar entidade de segurança existente",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal/update",
                    ServiceAPIRoute = "access/securityprincipal/update",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "principal", "Certify.Models.Hub.SecurityPrincipal" }
                    },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalUpdate)]
                },
                new()
                {
                    OperationName = "UpdateSecurityPrincipalAssignedRoles",
                    OperationMethod = HttpPost,
                    Comment = "Atualizar funções atribuídas para uma entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal/roles/update",
                    ServiceAPIRoute = "access/securityprincipal/roles/update",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "update", "Certify.Models.Hub.SecurityPrincipalAssignedRoleUpdate" }
                    },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalUpdateAssignedRoles)]
                },
                new()
                {
                    OperationName = "RemoveSecurityPrincipal",
                    OperationMethod = HttpDelete,
                    Comment = "Remover entidade de segurança",
                    PublicAPIController = "Access",
                    PublicAPIRoute = "securityprincipal",
                    ServiceAPIRoute = "access/securityprincipal/{id}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.SecurityPrincipal, StandardResourceActions.SecurityPrincipalDelete)]
                },
                new()
                {
                    OperationName = "AddHubManagedInstance",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar nova instância gerenciada ao hub",
                    ServiceAPIRoute = "managedinstance",
                    ReturnType = $"Models.Config.ActionResult<{nameof(Models.Hub.ManagedInstanceInfo)}>",
                    Params = new Dictionary<string, string> { { "item", nameof(Models.Hub.ManagedInstanceInfo) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceAdd)]
                },
                new()
                {
                    OperationName = "UpdateHubManagedInstance",
                    OperationMethod = HttpPost,
                    Comment = "Atualizar instância gerenciada existente no hub",
                    ServiceAPIRoute = "managedinstance/update",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "item", nameof(Models.Hub.ManagedInstanceInfo) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceUpdate)]
                },
                new()
                {
                    OperationName = "RemoveHubManagedInstance",
                    OperationMethod = HttpDelete,
                    Comment = "Remover instância gerenciada existente no hub",
                    PublicAPIController = "Hub",
                    PublicAPIRoute = "instances/{id}",
                    ServiceAPIRoute = "managedinstance/delete/{id}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceDelete)]
                },
                new()
                {
                    OperationName = "GetHubManagedInstance",
                    OperationMethod = HttpGet,
                    Comment = "Obter informações da instância gerenciada",
                    ServiceAPIRoute = "managedinstance/{id}",
                    ReturnType = nameof(Models.Hub.ManagedInstanceInfo),
                    Params = new Dictionary<string, string> { { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstancesList)]
                },
                new()
                {
                    OperationName = "GetHubManagedInstances",
                    OperationMethod = HttpGet,
                    Comment = "Obter instâncias gerenciadas",
                    ServiceAPIRoute = "managedinstance/list",
                    ReturnType = $"ICollection<{nameof(Models.Hub.ManagedInstanceInfo)}>",
                    Params = new Dictionary<string, string> { },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstancesList)]
                },
                new()
                {
                    OperationName = "GetManagedChallenges",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de desafios gerenciados disponíveis (delegação de desafio DNS etc)",
                    PublicAPIController = "ManagedChallenge",
                    PublicAPIRoute = "list",
                    ServiceAPIRoute = "managedchallenge",
                    ReturnType = "ICollection<ManagedChallenge>",
                    RequiredPermissions = [new(ResourceTypes.ManagedChallenge, StandardResourceActions.ManagedChallengeList)]
                },
                new()
                {
                    OperationName = "UpdateManagedChallenge",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar/atualizar um desafio gerenciado (delegação de desafio DNS etc)",
                    PublicAPIController = "ManagedChallenge",
                    PublicAPIRoute = "update",
                    ServiceAPIRoute = "managedchallenge",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "update", "Certify.Models.Hub.ManagedChallenge" }
                    },
                    RequiredPermissions = [new(ResourceTypes.ManagedChallenge, StandardResourceActions.ManagedChallengeUpdate)]
                },
                new()
                {
                    OperationName = "RemoveManagedChallenge",
                    OperationMethod = HttpDelete,
                    Comment = "Excluir um desafio gerenciado (delegação de desafio DNS etc)",
                    PublicAPIController = "ManagedChallenge",
                    PublicAPIRoute = "remove",
                    ServiceAPIRoute = "managedchallenge/{id}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "id", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.ManagedChallenge, StandardResourceActions.ManagedChallengeDelete)]
                },
                new()
                {
                    OperationName = "PerformManagedChallenge",
                    OperationMethod = HttpPost,
                    Comment = "Executar um desafio gerenciado (delegação de desafio DNS etc)",
                    PublicAPIController = null, // skip public controller implementation
                    ServiceAPIRoute = "managedchallenge/request",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "request", "Certify.Models.Hub.ManagedChallengeRequest" }
                    },
                    RequiredPermissions = [new(ResourceTypes.ManagedChallenge, StandardResourceActions.ManagedChallengeRequest)]
                },
                new()
                {
                    OperationName = "CleanupManagedChallenge",
                    OperationMethod = HttpPost,
                    Comment = "Executar limpeza para um desafio gerenciado previamente (delegação de desafio DNS etc)",
                    PublicAPIController = null, // skip public controller implementation
                    ServiceAPIRoute = "managedchallenge/cleanup",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "request", GetFormattedTypeName(typeof(Certify.Models.Hub.ManagedChallengeRequest)) }
                    },
                    RequiredPermissions = [new(ResourceTypes.ManagedChallenge, StandardResourceActions.ManagedChallengeCleanup)]
                },
                new()
                {
                    OperationName = "PerformExport",
                    OperationMethod = HttpPost,
                    Comment = "Realizar exportação de todas as configurações de uma instância",
                    ServiceAPIRoute = "system/migration/export",
                    ReturnType = GetFormattedTypeName(typeof(Models.Config.Migration.ImportExportPackage)),
                    Params = new Dictionary<string, string> { { "exportRequest", GetFormattedTypeName(typeof(Certify.Models.Config.Migration.ExportRequest)) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceExport)]
                },
                new()
                {
                    OperationName = "PerformImport",
                    OperationMethod = HttpPost,
                    Comment = "Realizar importação de todas as configurações de uma instância",
                    ServiceAPIRoute = "system/migration/import",
                    ReturnType = "ICollection<ActionStep>",
                    Params = new Dictionary<string, string> { { "importRequest", GetFormattedTypeName(typeof(Certify.Models.Config.Migration.ImportRequest)) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceImport)]
                },
                /* per instance API, via management hub */
                new()
                {
                    OperationName = "GetAcmeAccounts",
                    OperationMethod = HttpGet,
                    Comment = "Obter todas as Contas ACME",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/accounts/",
                    ReturnType = "ICollection<Certify.Models.AccountDetails>",
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.AcmeAccount, StandardResourceActions.AcmeAccountList)]
                },
                new()
                {
                    OperationName = "AddAcmeAccount",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar Nova Conta ACME",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/account/",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "registration", "Certify.Models.ContactRegistration" } },
                    RequiredPermissions = [new(ResourceTypes.AcmeAccount, StandardResourceActions.AcmeAccountAdd)]
                },
                new()
                {
                    OperationName = "GetCertificateAuthorities",
                    OperationMethod = HttpGet,
                    Comment = "Obter lista de Autoridades Certificadoras definidas",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/authority",
                    ReturnType = "ICollection<Certify.Models.CertificateAuthority>",
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.CertificateAuthority, StandardResourceActions.CertificateAuthorityList)]
                },
                new()
                {
                    OperationName = "UpdateCertificateAuthority",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar/Atualizar Autoridade Certificadora",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/authority",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "ca", "Certify.Models.CertificateAuthority" } },
                    RequiredPermissions = [new(ResourceTypes.CertificateAuthority, StandardResourceActions.CertificateAuthorityUpdate)]
                },
                new()
                {
                    OperationName = "RemoveCertificateAuthority",
                    OperationMethod = HttpDelete,
                    Comment = "Remover Autoridade Certificadora",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/authority/{id}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "id", "string" } },
                    RequiredPermissions = [new(ResourceTypes.CertificateAuthority, StandardResourceActions.CertificateAuthorityDelete)]
                },
                new()
                {
                    OperationName = "RemoveAcmeAccount",
                    OperationMethod = HttpDelete,
                    Comment = "Remover Conta ACME",
                    UseManagementAPI = true,
                    PublicAPIController = "CertificateAuthority",
                    PublicAPIRoute = "{instanceId}/accounts/{storageKey}/{deactivate}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "storageKey", "string" }, { "deactivate", "bool" } },
                    RequiredPermissions = [new(ResourceTypes.AcmeAccount, StandardResourceActions.AcmeAccountDelete)]
                },
                new()
                {
                    OperationName = "GetStoredCredentials",
                    OperationMethod = HttpGet,
                    Comment = "Obter Lista de Credenciais Armazenadas",
                    UseManagementAPI = true,
                    PublicAPIController = "StoredCredential",
                    PublicAPIRoute = "{instanceId}",
                    ReturnType = "ICollection<Certify.Models.Config.StoredCredential>",
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.StoredCredential, StandardResourceActions.StoredCredentialList)]
                },
                new()
                {
                    OperationName = "UpdateStoredCredential",
                    OperationMethod = HttpPost,
                    Comment = "Adicionar/Atualizar Credencial Armazenada",
                    PublicAPIController = "StoredCredential",
                    PublicAPIRoute = "{instanceId}",
                    ReturnType = actionResultTypeName,
                    UseManagementAPI = true,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "item", GetFormattedTypeName(typeof(Certify.Models.Config.StoredCredential)) } },
                    RequiredPermissions = [new(ResourceTypes.StoredCredential, StandardResourceActions.StoredCredentialUpdate)]
                },
                new()
                {
                    OperationName = "RemoveStoredCredential",
                    OperationMethod = HttpDelete,
                    Comment = "Remover Credencial Armazenada",
                    UseManagementAPI = true,
                    PublicAPIController = "StoredCredential",
                    PublicAPIRoute = "{instanceId}/{storageKey}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "storageKey", "string" } },
                    RequiredPermissions = [new(ResourceTypes.StoredCredential, StandardResourceActions.StoredCredentialDelete)]
                },
                new()
                {
                    OperationName = "UnlockStoredCredential",
                    OperationMethod = HttpPost,
                    Comment = "Desbloquear Credencial Armazenada",
                    UseManagementAPI = true,
                    PublicAPIController = "StoredCredential",
                    PublicAPIRoute = "{instanceId}/{storageKey}/unlock",
                    ReturnType = GetFormattedTypeName(typeof(Models.Config.StoredCredentialUnlockResult)),
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "storageKey", "string" } },
                    RequiredPermissions = [new(ResourceTypes.StoredCredential, StandardResourceActions.StoredCredentialReadSecret)]
                },
                new()
                {
                    OperationName = "GetDeploymentProviders",
                    OperationMethod = HttpGet,
                    Comment = "Obter Provedores de Tarefas de Implantação",
                    UseManagementAPI = true,
                    PublicAPIController = "DeploymentTask",
                    PublicAPIRoute = "{instanceId}",
                    ReturnType = "ICollection<Certify.Models.Config.DeploymentProviderDefinition>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.DeploymentTask, StandardResourceActions.DeploymentTaskListProviders)]
                },
                new()
                 {
                     OperationName = "GetTargetIPAddresses",
                     OperationMethod = HttpGet,
                     Comment = "Obter lista de endereços IP disponíveis no destino para vinculação de serviço (IIS, nginx etc)",
                     UseManagementAPI = true,
                     ManagementHubCommandType = Models.Hub.ManagementHubCommands.GetTargetIPAddresses,
                     PublicAPIController = "Target",
                     PublicAPIRoute = "{instanceId}/ipaddresses",
                     ReturnType = "ICollection<IPAddressOption>",
                     Params = new Dictionary<string, string>
                     {
                         { "instanceId", "string" }
                     },
                     RequiredPermissions = [new(ResourceTypes.Target, StandardResourceActions.TargetIPAddressesList)]
                 },
                new()
                {
                    OperationName = "GetTargetServiceTypes",
                    OperationMethod = HttpGet,
                    Comment = "Obter Tipos de Serviço presentes na instância (IIS, nginx etc)",
                    UseManagementAPI = true,
                    ManagementHubCommandType = Models.Hub.ManagementHubCommands.GetTargetServiceTypes,
                    PublicAPIController = "Target",
                    PublicAPIRoute = "{instanceId}/types",
                    ReturnType = "ICollection<string>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.Target, StandardResourceActions.TargetTypesList)]
                },
                new()
                {
                    OperationName = "GetTargetServiceItems",
                    OperationMethod = HttpGet,
                    Comment = "Obter itens de Serviço (sites) presentes na instância (IIS, nginx etc).",
                    UseManagementAPI = true,
                    ManagementHubCommandType = Models.Hub.ManagementHubCommands.GetTargetServiceItems,
                    PublicAPIController = "Target",
                    PublicAPIRoute = "{instanceId}/{serviceType}/items",
                    ReturnType = "ICollection<SiteInfo>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" },
                        { "serviceType", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.Target, StandardResourceActions.TargetServiceItemsList)]
                },
                new()
                {
                    OperationName = "GetTargetServiceItemIdentifiers",
                    OperationMethod = HttpGet,
                    Comment = "Obter identificadores de itens de Serviço (domínios em um site etc) presentes na instância (IIS, nginx etc)",
                    UseManagementAPI = true,
                    ManagementHubCommandType = Models.Hub.ManagementHubCommands.GetTargetServiceItemIdentifiers,
                    PublicAPIController = "Target",
                    PublicAPIRoute = "{instanceId}/{serviceType}/item/{itemId}/identifiers",
                    ReturnType = "ICollection<DomainOption>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" },
                        { "serviceType", "string" },
                        { "itemId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.Target, StandardResourceActions.TargetServiceItemIdentifiersList)]
                },
                new()
                {
                    OperationName = "GetChallengeProviders",
                    OperationMethod = HttpGet,
                    Comment = "Obter Provedores de Desafio DNS",
                    UseManagementAPI = true,
                    PublicAPIController = "ChallengeProvider",
                    PublicAPIRoute = "{instanceId}",
                    ReturnType = "ICollection<Certify.Models.Config.ChallengeProviderDefinition>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.ChallengeProvider, StandardResourceActions.ChallengeProviderList)]
                },
                new()
                {
                    OperationName = "GetDnsZones",
                    OperationMethod = HttpGet,
                    Comment = "Obter Lista de Zonas com o provedor DNS atual e credencial",
                    UseManagementAPI = true,
                    PublicAPIController = "ChallengeProvider",
                    PublicAPIRoute = "{instanceId}/dnszones/{providerTypeId}/{credentialId}",
                    ReturnType = "Certify.Models.Providers.DnsZoneQueryResult",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" },
                        { "providerTypeId", "string" },
                        { "credentialId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.ChallengeProvider, StandardResourceActions.ChallengeProviderDnsZonesList)]
                },
                new()
                {
                    OperationName = "ExecuteDeploymentTask",
                    OperationMethod = HttpGet,
                    Comment = "Executar Tarefa de Implantação",
                    UseManagementAPI = true,
                    PublicAPIController = "DeploymentTask",
                    PublicAPIRoute = "{instanceId}/execute/{managedCertificateId}/{taskId}",
                    ReturnType = "ICollection<ActionStep>",
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" },
                        { "managedCertificateId", "string" },
                        { "taskId", "string" }
                    },
                    RequiredPermissions = [new(ResourceTypes.DeploymentTask, StandardResourceActions.DeploymentTaskExecute)]
                },
                new()
                {
                    OperationName = "RemoveManagedCertificate",
                    OperationMethod = HttpDelete,
                    Comment = "Remover Certificado Gerenciado",
                    UseManagementAPI = true,
                    PublicAPIController = "Certificate",
                    PublicAPIRoute = "{instanceId}/settings/{managedCertId}",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "managedCertId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.ManagedItem, StandardResourceActions.ManagedItemDelete)]
                },
                new()
                {
                    OperationName = "PerformInstanceExport",
                    OperationMethod = HttpPost,
                    Comment = "Realizar exportação de todas as configurações",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/migration/export",
                    ReturnType =  GetFormattedTypeName(typeof(Models.Config.Migration.ImportExportPackage)),
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "exportRequest",  GetFormattedTypeName(typeof(Certify.Models.Config.Migration.ExportRequest)) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceExport)]
                },
                new()
                {
                    OperationName = "PerformInstanceImport",
                    OperationMethod = HttpPost,
                    Comment = "Realizar importação de todas as configurações",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/migration/import",
                    ReturnType = "ICollection<ActionStep>",
                    Params = new Dictionary<string, string> { { "instanceId", "string" }, { "importRequest",  GetFormattedTypeName(typeof(Certify.Models.Config.Migration.ImportRequest)) } },
                    RequiredPermissions = [new(ResourceTypes.ManagedInstance, StandardResourceActions.ManagementHubInstanceImport)]
                },
                new()
                {
                    OperationName = "GetInstanceStatusItems",
                    OperationMethod = HttpGet,
                    Comment = "Obter itens de status da instância",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/status",
                    ReturnType = "ICollection<ActionStep>",
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.System, StandardResourceActions.SystemStatusList)]
                },
                new()
                {
                    OperationName = "GetServiceConfig",
                    OperationMethod = HttpGet,
                    Comment = "Obter configuração de serviço para uma instância gerenciada",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/serviceconfig",
                    ReturnType = "Certify.Shared.ServiceConfig",
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.System, StandardResourceActions.SystemServiceConfigList)]
                },
                new()
                {
                    OperationName = "GetServiceCoreSettings",
                    OperationMethod = HttpGet,
                    Comment = "Obter configurações principais para uma instância gerenciada",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/coresettings",
                    ReturnType = nameof(Preferences),
                    Params = new Dictionary<string, string> { { "instanceId", "string" } },
                    RequiredPermissions = [new(ResourceTypes.System, StandardResourceActions.SystemCoreSettingsList)]
                },
                new()
                {
                    OperationName = "UpdateServiceConfig",
                    OperationMethod = HttpPost,
                    Comment = "Atualizar configuração de serviço da instância",
                    UseManagementAPI = true,
                    PublicAPIController = "System",
                    PublicAPIRoute = "{instanceId}/system/serviceconfig",
                    ReturnType = actionResultTypeName,
                    Params = new Dictionary<string, string>
                    {
                        { "instanceId", "string" },
                        { "config", "Certify.Shared.ServiceConfig" }
                    },
                    RequiredPermissions = [new(ResourceTypes.System, StandardResourceActions.SystemServiceConfigUpdate)]
                },
                 new()
                 {
                     OperationName = "UpdateServiceCoreSettings",
                     OperationMethod = HttpPost,
                     Comment = "Atualizar configurações principais de serviço da instância",
                     UseManagementAPI = true,
                     PublicAPIController = "System",
                     PublicAPIRoute = "{instanceId}/system/coresettings",
                     ReturnType = actionResultTypeName,
                     Params = new Dictionary<string, string>
                     {
                         { "instanceId", "string" },
                         { "prefs", "Certify.Models.Preferences" }
                     },
                     RequiredPermissions = [new(ResourceTypes.System, StandardResourceActions.SystemCoreSettingsUpdate)]
                 },
                 new()
                    {
                        OperationName = "GetHubItemTags",
                        OperationMethod = HttpGet,
                        Comment = "Obter tags de itens do hub",
                        PublicAPIController = "Hub",
                        PublicAPIRoute = "tags/list",
                        ServiceAPIRoute = "tags/list",
                        ReturnType = "ICollection<ItemTag>",
                        RequiredPermissions = [new(ResourceTypes.Tag, StandardResourceActions.TagList)]
                    },
                   new()
                  {
                      OperationName = "AddHubItemTag",
                      OperationMethod = HttpPost,
                      Comment = "Adicionar tag de item do hub",
                      PublicAPIController = "Hub",
                      PublicAPIRoute = "tags/add",
                      ServiceAPIRoute = "tags/add",
                      ReturnType = actionResultTypeName,
                      Params = new Dictionary<string, string> { { "tag", GetFormattedTypeName(typeof(ItemTag)) } },
                      RequiredPermissions = [new(ResourceTypes.Tag, StandardResourceActions.TagAdd)]
                  },
            };
        }
    }
}
