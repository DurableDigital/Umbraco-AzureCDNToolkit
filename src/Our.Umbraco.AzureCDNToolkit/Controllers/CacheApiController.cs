﻿using System.Linq;
using System.Threading;
using System;
using System.Web.Http;
using System.Collections.Generic;

using Newtonsoft.Json;

using Umbraco.Core.Services;
using Umbraco.Web.Cache;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Our.Umbraco.AzureCDNToolkit.CacheRefreshers;
using Our.Umbraco.AzureCDNToolkit.Models;

namespace Our.Umbraco.AzureCDNToolkit.Controllers
{

    [PluginController("AzureCDNToolkit")]
    public class CacheApiController : UmbracoAuthorizedApiController
    {

        private readonly IServerRegistrationService _serverRegistrationService;
        private readonly DistributedCache _distributedCache;

        public CacheApiController(IServerRegistrationService serverRegistrationService, DistributedCache distributedCache)
        {
            _serverRegistrationService = serverRegistrationService ?? throw new ArgumentNullException(nameof(serverRegistrationService));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        /// <summary>
        /// Sends a cache request message specifying a particular server return cache stats
        /// </summary>
        /// ~/Umbraco/backoffice/AzureCDNToolkit/CacheApi/SendCachedImagesRequest
        [HttpPost]
        [global::Umbraco.Web.WebApi.UmbracoAuthorize]
        public Guid SendCachedImagesRequest(string serverIdentity)
        {
            var thisRequestId = Guid.NewGuid();

            var thisRequest = new CachedImagesRequest
            {
                RequestId = thisRequestId,
                ServerIdentity = serverIdentity
            };

            var json = JsonConvert.SerializeObject(thisRequest);

            _distributedCache.RefreshByJson(CacheRequester.Guid, json);
            return thisRequestId;
        }

        /// <summary>
        /// Gets all cache image urls
        /// </summary>
        /// <returns>object</returns>
        /// ~/Umbraco/backoffice/AzureCDNToolkit/CacheApi/GetAllCachedImagesFromRequest
        [HttpPost]
        [global::Umbraco.Web.WebApi.UmbracoAuthorize]
        public IEnumerable<CachedImage> GetAllCachedImagesFromRequest(string requestId)
        {
            var cacheKey = $"{Constants.Keys.CachePrefixResponse}{requestId}";

            // it can take time for servers to return the data so try 6 times waiting 10 seconds between each try
            for (int retry = 0;; retry++)
            {
                var cachedResponse = Cache.GetCacheItem<IEnumerable<CachedImage>>(cacheKey);

                if (cachedResponse != null)
                {
                    return cachedResponse;
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));

                if (retry >= 6)
                {
                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// Wipes the image cache urls
        /// </summary>
        /// ~/Umbraco/backoffice/AzureCDNToolkit/CacheApi/WipeAll
        [HttpPost]
        [global::Umbraco.Web.WebApi.UmbracoAuthorize]
        public void Wipe(string serverIdentity, string webUrl = null)
        {
            var thisRequest = new CachedImagesWipe
            {
                ServerIdentity = serverIdentity,
                WebUrl = webUrl
            };

            var json = JsonConvert.SerializeObject(thisRequest);

            _distributedCache.RefreshByJson(CacheWiper.Guid, json);
        }

        /// <summary>
        /// Gets a collection of all servers from the ServerRegistrationService
        /// </summary>
        /// ~/Umbraco/backoffice/AzureCDNToolkit/CacheApi/GetAllServers
        public string[] GetAllServers()
        {
            // will try for 5 times waiting 3 seconds to get a list of servers as they can take time to register mainly when developing locally
            for (int retry = 0; ; retry++)
            {
                var serverDetails = _serverRegistrationService.GetActiveServers().ToList();
                if (serverDetails.Any())
                {
                    return serverDetails.Select(server => server.ServerIdentity).ToArray();
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));

                if (retry >= 5)
                {
                    break;
                }
            }

            return null;
        }

    }
}
