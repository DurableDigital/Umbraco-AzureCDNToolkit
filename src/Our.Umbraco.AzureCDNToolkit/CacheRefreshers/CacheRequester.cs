using System;
using Umbraco.Core.Cache;

namespace Our.Umbraco.AzureCDNToolkit.CacheRefreshers
{
    public class CacheRequester : JsonCacheRefresherBase<CacheRequester>
    {
        public CacheRequester(AppCaches appCaches) : base(appCaches)
        {
        }

        public static Guid Guid => new Guid("2A310ECC-D050-464D-9BED-2C9448255E01");

        public override string Name => "AzureCDNToolKitCacheReporter";

        protected override CacheRequester This => this;

        public override Guid RefresherUniqueId => Guid;
    }
}
