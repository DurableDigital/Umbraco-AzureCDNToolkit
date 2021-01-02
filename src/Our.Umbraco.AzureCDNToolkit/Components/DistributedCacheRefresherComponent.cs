using System;
using Newtonsoft.Json;
using Our.Umbraco.AzureCDNToolkit.CacheRefreshers;
using Our.Umbraco.AzureCDNToolkit.Models;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web.Cache;

namespace Our.Umbraco.AzureCDNToolkit.Components
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DistributedCacheRefresherComponent: IComponent
    {

        private readonly ILogger _logger;
        private readonly IServerRegistrationService _serverRegistrationService;
        private readonly DistributedCache _distributedCache;

        public DistributedCacheRefresherComponent(ILogger logger, IServerRegistrationService serverRegistrationService, DistributedCache distributedCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverRegistrationService = serverRegistrationService ?? throw new ArgumentNullException(nameof(serverRegistrationService));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public void Initialize()
        {
            CacheRefresherBase<CacheRequester>.CacheUpdated += CacheRequester_Request;
            CacheRefresherBase<CacheResponder>.CacheUpdated += CacheResponder_Response;
            CacheRefresherBase<CacheWiper>.CacheUpdated += CacheWiper_Request;
        }

        public void Terminate()
        {
            CacheRefresherBase<CacheRequester>.CacheUpdated -= CacheRequester_Request;
            CacheRefresherBase<CacheResponder>.CacheUpdated -= CacheResponder_Response;
            CacheRefresherBase<CacheWiper>.CacheUpdated -= CacheWiper_Request;
        }

        /// <summary>
        /// Handles all cache 'requests', and checks to see if the current machine should respond (with another 'cache refresher')
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CacheRequester_Request(CacheRequester sender, CacheRefresherEventArgs e)
        {
            var rawPayLoad = (string)e.MessageObject;

            var payload = JsonConvert.DeserializeObject<CachedImagesRequest>(rawPayLoad);

            if (
                _serverRegistrationService.CurrentServerIdentity.InvariantEquals(
                    payload.ServerIdentity))
            {
                // THIS SERVER SHOULD RETURN DATA VIA CacheImagesResponder

                var cachedItems = Cache.GetCacheItemsByKeySearch<CachedImage>(AzureCDNToolkit.Constants.Keys.CachePrefix);

                var response = new CachedImagesResponse()
                {
                    RequestId = payload.RequestId,
                    CachedImages = cachedItems
                };

                var json = JsonConvert.SerializeObject(response);

                _distributedCache.RefreshByJson(CacheResponder.Guid, json);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CacheResponder_Response(CacheResponder sender, CacheRefresherEventArgs e)
        {
            var rawPayLoad = (string)e.MessageObject;
            var payload = JsonConvert.DeserializeObject<CachedImagesResponse>(rawPayLoad);

            var cacheKey = $"{Constants.Keys.CachePrefixResponse}{payload.RequestId}";
            Cache.InsertCacheItem(cacheKey, () => payload.CachedImages);
        }


        /// <summary>
        /// Handles all cache 'requests', and checks to see if the current machine should respond (with another 'cache refresher')
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CacheWiper_Request(CacheWiper sender, CacheRefresherEventArgs e)
        {
            var rawPayLoad = (string)e.MessageObject;

            var payload = JsonConvert.DeserializeObject<CachedImagesWipe>(rawPayLoad);

            if (
                _serverRegistrationService.CurrentServerIdentity.InvariantEquals(
                    payload.ServerIdentity))
            {
                // This server should wipe it's application cache

                if (payload.WebUrl != null)
                {
                    // wipe specific url
                    var cachePrefix = Constants.Keys.CachePrefix;
                    var cacheKey = $"{cachePrefix}{payload.WebUrl}";
                    Cache.ClearCacheItem(cacheKey);

                    _logger.Info<DistributedCacheRefresherComponent>("Azure CDN Toolkit: CDN image path runtime cache for key {WebUrl} cleared by dashboard control request", payload.WebUrl);
                }
                else
                {
                    // clear all keys
                    Cache.ClearCacheByKeySearch(AzureCDNToolkit.Constants.Keys.CachePrefix);

                    _logger.Info<DistributedCacheRefresherComponent>("Azure CDN Toolkit: CDN image path runtime cache cleared by dashboard control request");
                }

            }
        }
    }
}
