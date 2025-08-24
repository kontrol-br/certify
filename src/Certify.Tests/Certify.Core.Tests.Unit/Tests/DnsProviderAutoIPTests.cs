using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Certify.Models.Providers;
using Certify.Providers.DNS.AutoIP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Certify.Core.Tests.Unit
{
    [TestClass]
    public class DnsProviderAutoIPTests
    {
        private class MockHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }

        private static async Task<(DnsProviderAutoIP provider, MockHandler handler)> SetupAsync(Dictionary<string, string> creds, Dictionary<string, string> parameters)
        {
            var provider = new DnsProviderAutoIP();
            await provider.InitProvider(creds, parameters, null);

            var handler = new MockHandler();
            var httpField = typeof(DnsProviderAutoIP).GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance);
            var existingHttp = (HttpClient)httpField!.GetValue(provider)!;
            var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Authorization = existingHttp.DefaultRequestHeaders.Authorization;
            httpField.SetValue(provider, http);
            return (provider, handler);
        }

        [TestMethod]
        public async Task CreateAndDelete_WithTokenAndHostname()
        {
            var (provider, handler) = await SetupAsync(
                new Dictionary<string, string> { ["acmetoken"] = "token" },
                new Dictionary<string, string> { ["hostname"] = "test.example.com" });

            var createResult = await provider.CreateRecord(new DnsRecord { RecordValue = "txtvalue" });
            var createContent = await handler.LastRequest!.Content!.ReadAsStringAsync();
            var createJson = JObject.Parse(createContent);
            Assert.AreEqual("txtvalue", createJson["txt"]!.ToString());
            Assert.AreEqual("test.example.com", createJson["hostname"]!.ToString());
            Assert.AreEqual(2, createJson.Count);
            Assert.IsTrue(createResult.IsSuccess);

            var deleteResult = await provider.DeleteRecord(new DnsRecord { RecordValue = "txtvalue" });
            var deleteContent = await handler.LastRequest!.Content!.ReadAsStringAsync();
            var deleteJson = JObject.Parse(deleteContent);
            Assert.AreEqual("txtvalue", deleteJson["txt"]!.ToString());
            Assert.AreEqual("test.example.com", deleteJson["hostname"]!.ToString());
            Assert.AreEqual(2, deleteJson.Count);
            Assert.IsTrue(deleteResult.IsSuccess);
        }

        [TestMethod]
        public async Task CreateAndDelete_WithUserPassword_NoHostname()
        {
            var (provider, handler) = await SetupAsync(
                new Dictionary<string, string> { ["username"] = "user", ["password"] = "pass" },
                new Dictionary<string, string>());

            var createResult = await provider.CreateRecord(new DnsRecord { RecordValue = "txtvalue" });
            Assert.IsFalse(createResult.IsSuccess);
            Assert.IsNull(handler.LastRequest);

            var deleteResult = await provider.DeleteRecord(new DnsRecord { RecordValue = "txtvalue" });
            Assert.IsFalse(deleteResult.IsSuccess);
            Assert.IsNull(handler.LastRequest);
        }

        [TestMethod]
        public async Task CreateAndDelete_MultipleHosts_SendsCorrectPayloads()
        {
            var (provider, handler) = await SetupAsync(
                new Dictionary<string, string> { ["acmetoken"] = "token" },
                new Dictionary<string, string> { ["hostname"] = "host0.example.com" });

            var hostnameField = typeof(DnsProviderAutoIP).GetField("_hostname", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var hosts = new[] { "host1.example.com", "host2.example.com" };

            for (var i = 0; i < hosts.Length; i++)
            {
                hostnameField.SetValue(provider, hosts[i]);

                var txt = $"txt{i}";
                var createResult = await provider.CreateRecord(new DnsRecord { RecordValue = txt });
                var createContent = await handler.LastRequest!.Content!.ReadAsStringAsync();
                var createJson = JObject.Parse(createContent);
                Assert.AreEqual(txt, createJson["txt"]!.ToString());
                Assert.AreEqual(hosts[i], createJson["hostname"]!.ToString());
                Assert.AreEqual(2, createJson.Count);
                Assert.IsTrue(createResult.IsSuccess);

                var deleteResult = await provider.DeleteRecord(new DnsRecord { RecordValue = txt });
                var deleteContent = await handler.LastRequest!.Content!.ReadAsStringAsync();
                var deleteJson = JObject.Parse(deleteContent);
                Assert.AreEqual(txt, deleteJson["txt"]!.ToString());
                Assert.AreEqual(hosts[i], deleteJson["hostname"]!.ToString());
                Assert.AreEqual(2, deleteJson.Count);
                Assert.IsTrue(deleteResult.IsSuccess);
            }
        }
    }
}
