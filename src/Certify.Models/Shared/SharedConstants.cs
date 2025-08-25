namespace Certify.Models
{
    public static class SharedConstants
    {
#if _DEMO_
        public const string APPDATASUBFOLDER = "AutoSSLDemo";
#else
        public const string APPDATASUBFOLDER = "AutoSSL";
#endif
    }
}
