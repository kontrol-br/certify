namespace Certify.Models.API
{
    public static class Config
    {
        public static string APIBaseURI { get; } = "https://update.autoip.com.br/v1/";
    }

    public class URLCheckResult
    {
        public bool IsAccessible { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; } = string.Empty;
    }
}
