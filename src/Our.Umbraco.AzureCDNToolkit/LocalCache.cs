using System;
using System.Collections.Generic;

using Umbraco.Core.Cache;

namespace Our.Umbraco.AzureCDNToolkit
{

    internal static class LocalCache
    {
        private static IAppPolicyCache GetRuntimeCache() => global::Umbraco.Core.Composing.Current.AppCaches.RuntimeCache;

        internal static T GetLocalCacheItem<T>(string cacheKey)
        {
            return GetRuntimeCache().GetCacheItem<T>(cacheKey);
        }

        internal static void InsertLocalCacheItem<T>(string cacheKey, Func<T> getCacheItem)
        {
            GetRuntimeCache().InsertCacheItem(cacheKey, getCacheItem);
        }

        internal static IEnumerable<T> GetLocalCacheItemsByKeySearch<T>(string keyStartsWith)
        {
            return GetRuntimeCache().GetCacheItemsByKeySearch<T>(keyStartsWith);
        }

        internal static void ClearLocalCacheItem(string key)
        {
            var runtimeCache = global::Umbraco.Core.Composing.Current.AppCaches.RuntimeCache;
            runtimeCache.Clear(key);
        }

        internal static void ClearLocalCacheByKeySearch(string keyStartsWith)
        {
            var runtimeCache = global::Umbraco.Core.Composing.Current.AppCaches.RuntimeCache;
            runtimeCache.ClearByKey(keyStartsWith);
        }
    }
}
