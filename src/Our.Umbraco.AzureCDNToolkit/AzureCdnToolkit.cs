using System.Web.Configuration;

namespace Our.Umbraco.AzureCDNToolkit
{
    public sealed class AzureCdnToolkit
    {
        /// <summary>
        /// singleton instance
        /// </summary>
        private static readonly AzureCdnToolkit azureCdnToolkit = new AzureCdnToolkit();

        /// <summary>
        /// Initialises static members of the <see cref="AzureCdnToolkit"/> class
        /// </summary>
        static AzureCdnToolkit()
        {
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="AzureCdnToolkit"/> class from being created
        /// (singleton constructor)
        /// </summary>
        private AzureCdnToolkit()
        {
            Refresh();
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="AzureCdnToolkit"/> class
        /// </summary>
        public static AzureCdnToolkit Instance
        {
            get
            {
                return azureCdnToolkit;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the CDN prefix should be used
        /// </summary>
        public bool UseAzureCdnToolkit { get; set; }

        /// <summary>
        /// Gets or sets the version of the assets to use for client side cache busting
        /// </summary>
        public string CdnPackageVersion { get; set; }

        /// <summary>
        /// Gets the CDN Url
        /// </summary>
        public string CdnUrl { get; set; }

        /// <summary>
        /// Gets the Assets Container
        /// </summary>
        public string AssetsContainer { get; set; }

        /// <summary>
        /// Gets the Media Container
        /// </summary>
        public string MediaContainer { get; set; }

        /// <summary>
        /// Determines wqhether to use Redis Cache or Local Cache
        /// </summary>
        public bool UseRedisCache { get; set; }

        /// <summary>
        /// Determines wqhether to use Redis Cache or Local Cache
        /// </summary>
        public string RedisCacheConnectionString { get; set; }

        /// <summary>
        /// Sets all properties
        /// </summary>

        internal string Domain { get; set; }

        public void Refresh()
        {

            if (WebConfigurationManager.AppSettings["AzureCDNToolkit:UseAzureCdnToolkit"] != null)
            {
                var useAzureCdnToolkit = bool.Parse(WebConfigurationManager.AppSettings["AzureCDNToolkit:UseAzureCdnToolkit"]);
                UseAzureCdnToolkit = useAzureCdnToolkit;
            }
            else
            {
                UseAzureCdnToolkit = true;
            }

            Domain = WebConfigurationManager.AppSettings["AzureCDNToolkit:Domain"];
            CdnPackageVersion = WebConfigurationManager.AppSettings["AzureCDNToolkit:CdnPackageVersion"];
            CdnUrl = WebConfigurationManager.AppSettings["AzureCDNToolkit:CdnUrl"];
            AssetsContainer = WebConfigurationManager.AppSettings["AzureCDNToolkit:AssetsContainer"] ?? "assets";
            MediaContainer = WebConfigurationManager.AppSettings["AzureCDNToolkit:MediaContainer"] ?? "media";
            UseRedisCache = WebConfigurationManager.AppSettings["AzureCDNToolkit:UseRedisCache"] != null &&
                                 bool.Parse(WebConfigurationManager.AppSettings["AzureCDNToolkit:UseRedisCache"]);
            RedisCacheConnectionString = WebConfigurationManager.AppSettings["AzureCDNToolkit:RedisCacheConnectionString"];
        }

    }
}
