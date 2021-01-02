using System;
using Umbraco.Core.Cache;

namespace Our.Umbraco.AzureCDNToolkit.CacheRefreshers
{
    public class CacheWiper : JsonCacheRefresherBase<CacheWiper>
    {
        public CacheWiper(AppCaches appCaches) : base(appCaches)
        {
        }

        public static Guid Guid => new Guid("8882A4B1-69C5-4B41-B578-C65E6F630A97");

        public override string Name => "AzureCDNToolKitCacheWiper";

        protected override CacheWiper This => this;

        public override Guid RefresherUniqueId => Guid;
    }
}
