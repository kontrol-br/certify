using System.Threading.Tasks;
using Certify.Models.Plugins;
using Certify.Models.Shared;

namespace Certify.Providers.Internal
{
    /// <summary>
    /// Stubbed dashboard client. All methods succeed without performing any
    /// network communication.
    /// </summary>
    public class DashboardClient : IDashboardClient
    {
        public Task<bool> SubmitFeedbackAsync(FeedbackReport feedback, string frameworkVersion) => Task.FromResult(true);

        public Task<bool> ReportRenewalStatusAsync(RenewalStatusReport report) => Task.FromResult(true);

        public Task<bool> ReportServerStatusAsync() => Task.FromResult(true);

        public Task<bool> SignInAsync(string email, string pwd) => Task.FromResult(true);

        public Task<bool> RegisterInstance(RegisteredInstance instance, string email, string pwd, bool createAccount) => Task.FromResult(true);

        public Task<bool> ReportUserActionRequiredAsync(ItemActionRequired actionRequired) => Task.FromResult(true);
    }
}
