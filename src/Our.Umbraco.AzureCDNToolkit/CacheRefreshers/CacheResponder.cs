using System;
using Umbraco.Core.Cache;

namespace Our.Umbraco.AzureCDNToolkit.CacheRefreshers
{
    public class CacheResponder : JsonCacheRefresherBase<CacheResponder>
    {
        public CacheResponder(AppCaches appCaches) : base(appCaches)
        {
        }

        public static Guid Guid => new Guid("A4EDE1C6-C73B-4DB2-ADC9-23C22B2152F9");

        public override string Name => "AzureCDNToolKitCacheResponder";

        protected override CacheResponder This => this;

        public override Guid RefresherUniqueId => Guid;
    }
}
