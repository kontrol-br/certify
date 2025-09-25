using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Certify.Models;
using Certify.Models.Config;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Win32;

namespace Certify.Management
{
    public class Util
    {

        /// <summary>
        /// check for problems which could affect app use
        /// </summary>
        /// <returns>  </returns>
        public static async Task<List<ActionResult>> PerformAppDiagnostics(bool includeTempFileCheck, string ntpServer = null)
        {
            var results = new List<ActionResult>();

            var tempFilePath = "";
            var tempFolder = Path.GetTempPath();

            // if current user can create temp files, attempt to create a 1MB temp file, detect if it fails
            if (includeTempFileCheck && !string.IsNullOrEmpty(tempFolder))
            {
                try
                {
                    tempFilePath = Path.GetTempFileName();

                    using (var fs = new FileStream(tempFilePath, FileMode.Open))
                    {
                        fs.Seek(1024 * 1024, SeekOrigin.Begin);
                        fs.WriteByte(0);
                        fs.Close();
                    }

                    File.Delete(tempFilePath);
                    results.Add(new ActionResult { IsSuccess = true, Message = $"Arquivo temporário de teste criado com sucesso." });
                }
                catch (Exception exp)
                {
                    results.Add(new ActionResult { Result = "tempfail", IsSuccess = false, Message = $"Não foi possível criar um arquivo temporário ({tempFilePath}). O Windows possui um limite de 65535 arquivos na pasta temporária ({tempFolder}). Limpe os arquivos temporários antes de prosseguir.{exp.Message}" });
                }
            }

            // check free disk space
            var limit = 512;
            try
            {
                var cDrive = new DriveInfo("c");
                if (cDrive.IsReady)
                {
                    var freeSpaceBytes = cDrive.AvailableFreeSpace;

                    // Check disk has at least <limit>MB free
                    if (freeSpaceBytes < (1024L * 1024 * limit))
                    {
                        results.Add(new ActionResult { Result = "lowdiskspace", IsSuccess = false, Message = $"A unidade C: possui menos de {limit}MB de espaço livre. O aplicativo pode não funcionar corretamente." });
                    }
                    else
                    {
                        results.Add(new ActionResult { IsSuccess = true, Message = $"A unidade C: possui mais de {limit}MB de espaço livre." });
                    }
                }
            }
            catch (Exception)
            {
                results.Add(new ActionResult { Result = "lowdiskspace", IsSuccess = false, Message = $"Não foi possível verificar quanto espaço em disco resta na unidade C:" });
            }

            // check internet time service, unless ntpServer pref set to ""

            if (ntpServer != "")
            {
                var timeResult = await CheckTimeServer(ntpServer);
                if (timeResult != null)
                {
                    var diff = timeResult - DateTimeOffset.UtcNow;

                    // if time is more than 50 seconds out, warn user, if beyond 100 days assume time server response is probably wrong (e.g. 01/01/1900)
                    if (Math.Abs(diff.Value.TotalSeconds) > 50 && Math.Abs(diff.Value.TotalDays) < 100)
                    {
                        results.Add(new ActionResult { IsSuccess = false, Message = $"Nota: O horário do seu sistema não parece estar sincronizado com um serviço de tempo da Internet, o que pode resultar em erros de solicitação de certificado." });
                    }
                    else
                    {
                        results.Add(new ActionResult { IsSuccess = true, Message = $"O horário do sistema está correto." });
                    }
                }
                else
                {
                    // could not perform test, assume firewall limitation and assume user is syncing their time.
                    results.Add(new ActionResult { IsSuccess = true, Message = $"Nota: Não foi possível confirmar que o horário do sistema está correto usando o servidor NTP ({ntpServer ?? "pool.ntp.org (default, UDP port 123)"}). Você deve garantir que o horário do sistema esteja sempre correto para evitar erros de solicitação de certificado." });
                }
            }

            // check if FIPS is enabled
            try
            {
                _ = SHA256.Create();
            }
            catch (Exception)
            {
                // if creating managed SHA256 fails may be FIPS validation
                results.Add(new ActionResult { IsSuccess = false, Message = $"Seu sistema não pode criar uma instância de criptografia SHA256. Você pode ter ativado inadvertidamente o FIPS, o que impede o uso de algumas funções criptográficas padrão no .Net - recursos como a verificação de atualizações do aplicativo não funcionarão. " });
            }

            // check powershell version
            var subkey = @"SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine";
            var isPSAvailable = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(subkey))
                    {
                        var vals = (ndpKey.GetValue("PSCompatibleVersion") as string).Split(',');
                        if (!vals.Any(v => v.Trim() == "5.0"))
                        {
                            isPSAvailable = false;
                        }
                    }
                }
                catch
                {
                    isPSAvailable = false;
                }
            }
            else
            {
                isPSAvailable = false; // assume PowerShell not present on non-windows
            }

            if (!isPSAvailable)
            {
                results.Add(new ActionResult { IsSuccess = false, Message = $"O PowerShell 5.0 ou superior é necessário para algumas funcionalidades e não parece estar disponível neste sistema. Consulte https://docs.microsoft.com/en-us/powershell/scripting/windows-powershell/install/windows-powershell-system-requirements" });
            }
            else
            {
                results.Add(new ActionResult { IsSuccess = true, Message = $"O PowerShell 5.0 ou superior está disponível." });
            }

            return results;
        }

        public static void SetSupportedTLSVersions()
        {
#if NET9_0_OR_GREATER
            return;
#else
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
                return;
            }
            catch
            {
                Debug.WriteLine("ServicePointManager.SecurityProtocol : Não foi possível selecionar o TLS 1.3 como protocolo compatível");
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            Debug.WriteLine("ServicePointManager.SecurityProtocol : O TLS 1.2 é o protocolo compatível mais alto");
#endif
        }

        public static string GetUserAgent()
        {
            var versionName = "Certify/" + GetAppVersion().ToString();
            return $"{versionName} ({RuntimeInformation.OSDescription}; {Environment.OSVersion}) ";
        }

        public static Version GetAppVersion()
        {
            // returns the version of Certify.Shared
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            var v = assembly.GetName().Version;
            return v;
        }

        public async Task<UpdateCheck> CheckForUpdates()
        {
            var v = GetAppVersion();
            return await CheckForUpdates(v);
        }

        public async Task<UpdateCheck> CheckForUpdates(Version appVersion) => await CheckForUpdates(appVersion.ToString());

        public async Task<UpdateCheck> CheckForUpdates(string appVersion)
        {
            //get app version
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", Util.GetUserAgent());

                    // https://update.autoip.com.br/v1/update?version={appVersion}
                    var updateUri = new Uri(new Uri(Models.API.Config.APIBaseURI), $"update?version={appVersion}");
                    var response = await client.GetAsync(updateUri);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        /*json = @"{
                             'version': {
                                 'major': 2,
                                 'minor': 0,
                                 'patch': 3
                                                     },
                               'message': {
                                                         'body': 'There is an awesome update available.',
                                 'downloadPageURL': 'https://certify.webprofusion.com',
                                 'releaseNotesURL': 'https://certify.webprofusion.com/home/changelog',
                                 'isMandatory': true
                               }
                         }";*/

                        var checkResult = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateCheck>(json);
                        return CompareVersions(appVersion, checkResult);
                    }

                    return new UpdateCheck { IsNewerVersion = false, InstalledVersion = AppVersion.FromString(appVersion) };
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static UpdateCheck CompareVersions(string appVersion, UpdateCheck checkResult)
        {
            checkResult.IsNewerVersion = AppVersion.IsOtherVersionNewer(AppVersion.FromString(appVersion), checkResult.Version);

            // check for mandatory updates
            if (checkResult.Message != null && checkResult.Message.MandatoryBelowVersion != null)
            {
                checkResult.MustUpdate = AppVersion.IsOtherVersionNewer(AppVersion.FromString(appVersion), checkResult.Message.MandatoryBelowVersion);
            }

            checkResult.InstalledVersion = AppVersion.FromString(appVersion);

            return checkResult;
        }

        public string GetFileSHA256(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                SHA256 sha = null;

                try
                {
                    sha = System.Security.Cryptography.SHA256.Create();
                }
                catch (System.InvalidOperationException)
                {
                    // system probably has FIPS enabled and doesn't support standard SHA256
                    return null;
                }

                var checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower();
            }
        }

        public bool VerifyUpdateFile(string tempFile, string expectedHash, bool throwOnDeviation = true)
        {
            //verify file SHA256
            string computedSHA256 = null;
            using (Stream stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true))
            {
                computedSHA256 = GetFileSHA256(stream);
            }

            bool hashVerified;

            if (expectedHash.ToLower() == computedSHA256)
            {
                hashVerified = true;
            }
            else
            {
                if (throwOnDeviation)
                {
                    throw new Exception("O arquivo baixado falhou na verificação de hash SHA256");
                }
                else
                {
                    hashVerified = false;
                }
            }

            return hashVerified;
        }

        public static string GetUserLocalAppDataFolder()
        {
            // allow override via CERTIFY_APPDATA_PATH environment variable
            var envPath = Environment.GetEnvironmentVariable("CERTIFY_APPDATA_PATH");
            var path = !string.IsNullOrEmpty(envPath)
                ? envPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Models.SharedConstants.APPDATASUBFOLDER);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Create a local app data folder. This method will throw an exception if any IO operations fail.
        /// </summary>
        /// <param name="folder">subfolder to create</param>
        /// <returns></returns>
        public string CreateLocalAppDataPath(string folder)
        {
            // create a new temp folder under our Local User %APPDATA% folder for access by our current user
            var appData = GetUserLocalAppDataFolder();

            var destPath = Path.Combine(appData, folder);

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            return destPath;
        }

        public async Task<UpdateCheck> DownloadUpdate()
        {
            var result = await CheckForUpdates();

#if DEBUG
            result.IsNewerVersion = true;
#endif
            if (result.IsNewerVersion)
            {
                string updatePath;

                try
                {
                    updatePath = CreateLocalAppDataPath("updates");
                }
                catch (Exception)
                {
                    throw new Exception("Falha ao baixar a atualização. Não foi possível criar a pasta temporária em %APPDATA%");
                }

                //https://github.com/dotnet/corefx/issues/6849
                var tempFile = Path.Combine(new string[] { updatePath, "Certify_" + result.Version.ToString() + "_Setup.tmp" });
                var setupFile = tempFile.Replace(".tmp", ".exe");

                var downloadVerified = false;
                if (File.Exists(setupFile))
                {
                    // file already downloaded, see if it's already valid
                    if (VerifyUpdateFile(setupFile, result.Message.SHA256, throwOnDeviation: true))
                    {
                        downloadVerified = true;
                    }
                }

                if (!downloadVerified)
                {

                    // attempt cleanup of all old setup files
                    var dirInfo = new DirectoryInfo(updatePath);
                    var setupFiles = dirInfo.GetFiles("*.exe")
                                         .Where(p => p.Extension == ".exe")
                                         .ToArray();
                    foreach (var file in setupFiles)
                    {
                        try
                        {
                            File.Delete(file.FullName);
                        }
                        catch { }
                    }

                    // download and verify new setup
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", Util.GetUserAgent());

                            using (var response = client.GetAsync(result.Message.DownloadFileURL, HttpCompletionOption.ResponseHeadersRead).Result)
                            {
                                response.EnsureSuccessStatusCode();

                                using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                {
                                    var totalRead = 0L;
                                    var totalReads = 0L;
                                    var buffer = new byte[8192];
                                    var isMoreToRead = true;

                                    do
                                    {
                                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                        if (read == 0)
                                        {
                                            isMoreToRead = false;
                                        }
                                        else
                                        {
                                            await fileStream.WriteAsync(buffer, 0, read);

                                            totalRead += read;
                                            totalReads += 1;

                                            if (totalReads % 512 == 0)
                                            {
                                                Console.WriteLine(string.Format("total de bytes baixados até agora: {0:n0}", totalRead));
                                            }
                                        }
                                    }
                                    while (isMoreToRead);
                                    fileStream.Close();
                                }
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        System.Diagnostics.Debug.WriteLine("Falha ao baixar a atualização: " + exp.ToString());
                        downloadVerified = false;
                    }
                    // verify temp file
                    if (!downloadVerified && VerifyUpdateFile(tempFile, result.Message.SHA256, throwOnDeviation: true))
                    {
                        downloadVerified = true;
                        if (File.Exists(setupFile))
                        {
                            File.Delete(setupFile); //delete existing file
                        }

                        File.Move(tempFile, setupFile); // final setup file
                    }
                }

                if (downloadVerified)
                {
                    // setup is ready to run
                    result.UpdateFilePath = setupFile;
                }
            }

            return result;
        }

        /// <summary>
        /// From https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
        /// </summary>
        /// <returns>  </returns>
        public static string GetDotNetVersion()
        {
            return RuntimeInformation.FrameworkDescription;
        }

        private static string GetDotNetVersion(int releaseKey)
        {
            if (releaseKey >= 528040)
            {
                return "4.8 ou posterior";
            }

            if (releaseKey >= 461808)
            {
                return "4.7.2";
            }

            if (releaseKey >= 461308)
            {
                return "4.7.1";
            }

            if (releaseKey >= 460798)
            {
                return "4.7";
            }

            if (releaseKey >= 460798)
            {
                return "4.7";
            }

            if (releaseKey >= 394802)
            {
                return "4.6.2";
            }

            if (releaseKey >= 394254)
            {
                return "4.6.1";
            }

            if (releaseKey >= 393295)
            {
                return "4.6";
            }

            if (releaseKey >= 379893)
            {
                return "4.5.2";
            }

            if (releaseKey >= 378675)
            {
                return "4.5.1";
            }

            if (releaseKey >= 378389)
            {
                return "4.5";
            }

            // This code should never execute. A non-null release key should mean that 4.5 or later
            // is installed.
            return "Nenhuma versão 4.5 ou posterior detectada";
        }

        public static string ToUrlSafeBase64String(byte[] data)
        {
            var s = Convert.ToBase64String(data);
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        public static string ToUrlSafeBase64String(string val)
        {
            var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(val);
            return ToUrlSafeBase64String(bytes);
        }

        public static byte[] FromUrlSafeBase64String(string urlSafeBase64)
        {
            if (string.IsNullOrEmpty(urlSafeBase64))
            {
                return [];
            }

            // Replace URL-safe characters back to standard base64 characters
            var base64 = urlSafeBase64.Replace('-', '+').Replace('_', '/');

            // Add padding if necessary
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("String Base64 segura para URL inválida", nameof(urlSafeBase64));
            }
        }

        public static async Task<DateTimeOffset?> CheckTimeServer(string ntpServer = "pool.ntp.org")
        {
            // https://stackoverflow.com/questions/1193955/how-to-query-an-ntp-server-using-c

            if (ntpServer == null)
            {
                ntpServer = "pool.ntp.org";
            }

            try
            {

                const int DaysTo1900 = 1900 * 365 + 95; // 95 = offset for leap-years etc.
                const long TicksPerSecond = 10000000L;
                const long TicksPerDay = 24 * 60 * 60 * TicksPerSecond;
                const long TicksTo1900 = DaysTo1900 * TicksPerDay;

                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                var ipEndPoint = new IPEndPoint(addresses[0], 123);
                var pingDuration = Stopwatch.GetTimestamp(); // temp access (JIT-Compiler need some time at first call)

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    await socket.ConnectAsync(ipEndPoint);
                    socket.ReceiveTimeout = 5000;
                    socket.Send(ntpData);
                    pingDuration = Stopwatch.GetTimestamp(); // after Send-Method to reduce WinSocket API-Call time

                    socket.Receive(ntpData);
                    pingDuration = Stopwatch.GetTimestamp() - pingDuration;
                }

                var pingTicks = pingDuration * TicksPerSecond / Stopwatch.Frequency;

                // optional: display response-time
                // Console.WriteLine("{0:N2} ms", new TimeSpan(pingTicks).TotalMilliseconds);

                var intPart = (long)ntpData[40] << 24 | (long)ntpData[41] << 16 | (long)ntpData[42] << 8 | ntpData[43];
                var fractPart = (long)ntpData[44] << 24 | (long)ntpData[45] << 16 | (long)ntpData[46] << 8 | ntpData[47];
                var netTicks = intPart * TicksPerSecond + (fractPart * TicksPerSecond >> 32);

                var networkDateTime = new DateTime(TicksTo1900 + netTicks + pingTicks / 2, DateTimeKind.Utc);

                return new DateTimeOffset(networkDateTime);
            }
            catch
            {
                // fail
                return null;
            }
        }
    }

    public class TelemetryManager : IDisposable
    {
        private TelemetryConfiguration _config = TelemetryConfiguration.CreateDefault();
        private TelemetryClient _tc = null;

        public TelemetryManager(string key)
        {
            InitTelemetry(key);
        }

        ~TelemetryManager()
        {
            if (_config != null)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            _config?.Dispose();
            _config = null;
            _tc = null;
        }

        public void InitTelemetry(string key)
        {
            _config = TelemetryConfiguration.CreateDefault();
            _config.ConnectionString = $"InstrumentationKey={key}";

            _tc = new TelemetryClient(_config);

            // Set session data:

            _tc.Context.Session.Id = Guid.NewGuid().ToString();
            _tc.Context.Component.Version = Util.GetAppVersion().ToString();
            _tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            _tc?.TrackEvent(eventName, properties);
        }

        public void TrackException(Exception exp, IDictionary<string, string> properties = null)
        {
            _tc?.TrackException(exp, properties);
        }
    }
}
