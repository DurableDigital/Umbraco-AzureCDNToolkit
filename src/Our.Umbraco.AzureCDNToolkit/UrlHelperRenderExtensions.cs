﻿using System;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Net.Http;
using System.Web.Configuration;

using Newtonsoft.Json;
using Our.Umbraco.AzureCDNToolkit.Models;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors.ValueConverters;

namespace Our.Umbraco.AzureCDNToolkit
{
    public static class UrlHelperRenderExtensions
    {
        public static IHtmlString GetCropCdnUrl(this UrlHelper urlHelper,
            ImageCropperValue imageCropperValue,
            int? width = null,
            int? height = null,
            string cropAlias = null,
            int? quality = null,
            ImageCropMode? imageCropMode = null,
            ImageCropAnchor? imageCropAnchor = null,
            bool preferFocalPoint = false,
            bool useCropDimensions = false,
            string cacheBusterValue = null,
            string furtherOptions = null,
            ImageCropRatioMode? ratioMode = null,
            bool upScale = true,
            bool htmlEncode = true
            )
        {

            // if no cacheBusterValue provided we need to make one
            cacheBusterValue ??= DateTime.UtcNow.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);

            var cropUrl = urlHelper.GetCropUrl(imageCropperValue, width, height, cropAlias, quality, imageCropMode,
                imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBusterValue, furtherOptions, ratioMode,
                upScale, false).ToString();

            return UrlToCdnUrl(cropUrl, htmlEncode);
        }

        public static IHtmlString GetCropCdnUrl(this UrlHelper urlHelper,
            IPublishedContent mediaItem,
            int? width = null,
            int? height = null,
            string propertyAlias = global::Umbraco.Core.Constants.Conventions.Media.File,
            string cropAlias = null,
            int? quality = null,
            ImageCropMode? imageCropMode = null,
            ImageCropAnchor? imageCropAnchor = null,
            bool preferFocalPoint = false,
            bool useCropDimensions = false,
            bool cacheBuster = true,
            string furtherOptions = null,
            ImageCropRatioMode? ratioMode = null,
            bool upScale = true,
            bool htmlEncode = true
            )
        {
            var cropUrl = urlHelper.GetCropUrl(mediaItem, width, height, propertyAlias, cropAlias, quality, imageCropMode,
                imageCropAnchor, preferFocalPoint, useCropDimensions, cacheBuster, furtherOptions, ratioMode,
                upScale, false).ToString();

            return UrlToCdnUrl(cropUrl, htmlEncode);
        }

        public static IHtmlString ResolveCdn(this UrlHelper urlHelper, string path, bool asset = true, bool htmlEncode = true)
        {
            return ResolveCdn(urlHelper, path, AzureCdnToolkit.Instance.CdnPackageVersion, asset, htmlEncode: htmlEncode);
        }

        // Special version of the method with fallback image for TinyMCE converter
        internal static IHtmlString ResolveCdnFallback(this UrlHelper urlHelper, IPublishedContent mediaItem, bool asset = true, string querystring = null, bool htmlEncode = true, string fallbackImage = null)
        {
            Current.Logger.Debug(typeof(UrlHelperRenderExtensions), "Parsed out media item from TinyMCE: {Name}", mediaItem.Name);
            var std = ResolveCdn(urlHelper, mediaItem, asset, querystring, htmlEncode);
            return std ?? ResolveCdn(urlHelper, fallbackImage, asset, htmlEncode);
        }

        public static IHtmlString ResolveCdn(this UrlHelper urlHelper, IPublishedContent mediaItem, bool asset = true, string querystring = null, bool htmlEncode = true)
        {
            var cacheBusterValue = mediaItem.UpdateDate.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture);

            var path = mediaItem.Url();

            // If mediaItem.Url is null attempt to get from Cropper
            if (path == null)
            {
                // attempt to get value from Cropper if there is one
                var umbracoFile = mediaItem.GetProperty(global::Umbraco.Core.Constants.Conventions.Media.File).GetSourceValue().ToString();
                if (!string.IsNullOrEmpty(umbracoFile))
                {
                    var cropper = JsonConvert.DeserializeObject<ImageCropperValue>(umbracoFile);
                    if (cropper != null)
                    {
                        path = cropper.Src;
                    }
                }
                else
                {
                    // unable to get a Url for the media item
                    return null;
                }
            }

            if (querystring != null)
            {
                path = $"{path}?{querystring}";
            }

            return ResolveCdn(urlHelper, path, cacheBusterValue, asset, "rnd", htmlEncode);
        }

        public static IHtmlString ResolveCdn(this UrlHelper urlHelper, string path, string cacheBuster, bool asset = true, string cacheBusterName = "v", bool htmlEncode = true)
        {
            if (AzureCdnToolkit.Instance.UseAzureCdnToolkit)
            {
                var absoluteDomain = AzureCdnToolkit.Instance.Domain ?? HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                bool wasAbsolute = Uri.TryCreate(path, UriKind.Absolute, out var srcUri);

                if (srcUri == null)
                {
                    // relative url that we need to make absolute so that Uri works properly
                    Uri.TryCreate($"{absoluteDomain}{path}", UriKind.Absolute, out srcUri);
                }

                var qs = srcUri.ParseQueryString();

                if (wasAbsolute && $"{srcUri.Scheme}://{srcUri.DnsSafeHost}" != absoluteDomain)
                {
                    // absolute url already and not this site - abort!
                    return new HtmlString(path);
                }

                string cdnPath;

                if (asset && !path.InvariantContains("/media/"))
                {
                    cdnPath = $"{AzureCdnToolkit.Instance.CdnUrl}/{AzureCdnToolkit.Instance.AssetsContainer}";
                }
                else
                {
                    cdnPath = AzureCdnToolkit.Instance.CdnUrl;
                }

                // Check if we should add version cachebuster
                if (qs["v"] == null && qs["rnd"] == null)
                {
                    qs.Add(cacheBusterName, cacheBuster);
                    path = $"{srcUri.LocalPath}?{qs}";
                }

                bool hasContext = false;
                var umbracoContextFactory = DependencyResolver.Current.GetService<IUmbracoContextFactory>();
                if (umbracoContextFactory != null)
                    using (var contextReference = umbracoContextFactory.EnsureUmbracoContext())
                        hasContext = contextReference.UmbracoContext != null;

                // TRY for ImageProcessor Azure Cache and check for ApplicationContext.Current (otherwise a test)
                if (!asset && hasContext)
                {
                    var azureCachePath = UrlToCdnUrl(path, false, absoluteDomain).ToString();

                    if (!azureCachePath.InvariantEquals($"{absoluteDomain}{path}"))
                    {
                        path = azureCachePath;
                    }
                }
                else if (!asset && qs.AllKeys.Any(x => !x.InvariantEquals("v") && !x.InvariantEquals("rnd")))
                {
                    // check if has querystring excluding cachebusters, if it does return as is as ImageProcessor needs to process
                }
                else
                {
                    // direct request to CDN, should remove all querystrings except cachebuster

                    // Adjust for custom media container names
                    if (!AzureCdnToolkit.Instance.MediaContainer.InvariantEquals("media"))
                    {
                        srcUri = new Uri(srcUri.AbsoluteUri.Replace("/media/", $"/{AzureCdnToolkit.Instance.MediaContainer}/"));
                    }

                    if (qs["rnd"] != null)
                    {
                        cacheBusterName = "rnd";
                    }
                    path = $"{cdnPath}{srcUri.LocalPath}?{cacheBusterName}={cacheBuster}";
                }
            }

            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(path)) : new HtmlString(path);

        }

        internal static IHtmlString UrlToCdnUrl(string cropUrl, bool htmlEncode, string currentDomain = null)
        {
            if (string.IsNullOrEmpty(cropUrl))
            {
                return new HtmlString(string.Empty);
            }

            // If toolkit disabled return original string
            if (!AzureCdnToolkit.Instance.UseAzureCdnToolkit)
            {
                return new HtmlString(cropUrl);
            }

            if (string.IsNullOrEmpty(currentDomain))
            {
                currentDomain = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            }

            var cachePrefix = Constants.Keys.CachePrefix;
            var cacheKey = $"{cachePrefix}{cropUrl}";

            var absoluteCropPath = $"{currentDomain}{cropUrl}";

            var cachedItem = Cache.GetCacheItem<CachedImage>(cacheKey);

            var fullUrlPath = string.Empty;

            try
            {
                if (cachedItem == null)
                {
                    var newCachedImage = new CachedImage { WebUrl = cropUrl };

                    Current.Logger.Debug(typeof(UrlHelperRenderExtensions), "Attempting to resolve: {Path}", absoluteCropPath);

                    // if security token has been setup we need to add it here
                    var securityToken = WebConfigurationManager.AppSettings["AzureCDNToolkit:SecurityToken"];
                    if (!string.IsNullOrEmpty(securityToken))
                    {
                        absoluteCropPath = $"{absoluteCropPath}&securitytoken={securityToken}";
                    }

                    // Retry five times before giving up to account for networking issues
                    TryFiveTimes(() =>
                    {
                        var request = (HttpWebRequest)WebRequest.Create(absoluteCropPath);
                        request.Method = "HEAD";
                        using var response = (HttpWebResponse)request.GetResponse();
                        var responseCode = response.StatusCode;
                        if (responseCode.Equals(HttpStatusCode.OK))
                        {
                            var absoluteUri = response.ResponseUri.AbsoluteUri;
                            newCachedImage.CacheUrl = absoluteUri;

                            // this is to mark URLs returned direct to Blob by ImageProcessor as not fully resolved
                            newCachedImage.Resolved = absoluteUri.InvariantContains(AzureCdnToolkit.Instance.CdnUrl);

                            Cache.InsertCacheItem(cacheKey, () => newCachedImage);
                            fullUrlPath = response.ResponseUri.AbsoluteUri;
                        }
                    });

                }
                else
                {
                    fullUrlPath = cachedItem.CacheUrl;
                }
            }
            catch (Exception ex)
            {
                Current.Logger.Error(typeof(UrlHelperRenderExtensions), ex, "Error resolving media url from the CDN");

                // we have tried 5 times and failed so let's cache the normal address
                var newCachedImage = new CachedImage
                {
                    WebUrl = cropUrl,
                    Resolved = false,
                    CacheUrl = cropUrl
                };
                Cache.InsertCacheItem(cacheKey, () => newCachedImage);

                fullUrlPath = cropUrl;
            }

            return htmlEncode ? new HtmlString(HttpUtility.HtmlEncode(fullUrlPath)) : new HtmlString(fullUrlPath);
        }

        /// <summary>
        /// Tries to execute a delegate action five times.
        /// </summary>
        /// <param name="delegateAction">The delegate to be executed</param>
        private static void TryFiveTimes(Action delegateAction)
        {
            for (int retry = 0; ; retry++)
            {
                try
                {
                    delegateAction();
                    return;
                }
                catch
                {
                    if (retry >= 5)
                    {
                        throw;
                    }
                }
            }
        }

        // This method is copied from Umbraco v7.4 as we need to support Umbraco v7.3 for the time being
        //private static string GetCropUrl(
        //    this string imageUrl,
        //    ImageCropDataSet cropDataSet,
        //    int? width = null,
        //    int? height = null,
        //    string cropAlias = null,
        //    int? quality = null,
        //    ImageCropMode? imageCropMode = null,
        //    ImageCropAnchor? imageCropAnchor = null,
        //    bool preferFocalPoint = false,
        //    bool useCropDimensions = false,
        //    string cacheBusterValue = null,
        //    string furtherOptions = null,
        //    ImageCropRatioMode? ratioMode = null,
        //    bool upScale = true)
        //{
        //    if (string.IsNullOrEmpty(imageUrl) == false)
        //    {
        //        var imageProcessorUrl = new StringBuilder();

        //        if (cropDataSet != null && (imageCropMode == ImageCropMode.Crop || imageCropMode == null))
        //        {
        //            var crop = cropDataSet.GetCrop(cropAlias);

        //            imageProcessorUrl.Append(cropDataSet.Src);

        //            var cropBaseUrl = cropDataSet.GetCropBaseUrl(cropAlias, preferFocalPoint);
        //            if (cropBaseUrl != null)
        //            {
        //                imageProcessorUrl.Append(cropBaseUrl);
        //            }
        //            else
        //            {
        //                return null;
        //            }

        //            if (crop != null & useCropDimensions)
        //            {
        //                width = crop.Width;
        //                height = crop.Height;
        //            }

        //            // If a predefined crop has been specified & there are no coordinates & no ratio mode, but a width parameter has been passed we can get the crop ratio for the height
        //            if (crop != null && string.IsNullOrEmpty(cropAlias) == false && crop.Coordinates == null && ratioMode == null && width != null && height == null)
        //            {
        //                var heightRatio = (decimal)crop.Height / (decimal)crop.Width;
        //                imageProcessorUrl.Append("&heightratio=" + heightRatio.ToString(CultureInfo.InvariantCulture));
        //            }

        //            // If a predefined crop has been specified & there are no coordinates & no ratio mode, but a height parameter has been passed we can get the crop ratio for the width
        //            if (crop != null && string.IsNullOrEmpty(cropAlias) == false && crop.Coordinates == null && ratioMode == null && width == null && height != null)
        //            {
        //                var widthRatio = (decimal)crop.Width / (decimal)crop.Height;
        //                imageProcessorUrl.Append("&widthratio=" + widthRatio.ToString(CultureInfo.InvariantCulture));
        //            }
        //        }
        //        else
        //        {
        //            imageProcessorUrl.Append(imageUrl);

        //            if (imageCropMode == null)
        //            {
        //                imageCropMode = ImageCropMode.Pad;
        //            }

        //            imageProcessorUrl.Append("?mode=" + imageCropMode.ToString().ToLower());

        //            if (imageCropAnchor != null)
        //            {
        //                imageProcessorUrl.Append("&anchor=" + imageCropAnchor.ToString().ToLower());
        //            }
        //        }

        //        if (quality != null)
        //        {
        //            imageProcessorUrl.Append("&quality=" + quality);
        //        }

        //        if (width != null && ratioMode != ImageCropRatioMode.Width)
        //        {
        //            imageProcessorUrl.Append("&width=" + width);
        //        }

        //        if (height != null && ratioMode != ImageCropRatioMode.Height)
        //        {
        //            imageProcessorUrl.Append("&height=" + height);
        //        }

        //        if (ratioMode == ImageCropRatioMode.Width && height != null)
        //        {
        //            // if only height specified then assume a sqaure
        //            if (width == null)
        //            {
        //                width = height;
        //            }

        //            var widthRatio = (decimal)width / (decimal)height;
        //            imageProcessorUrl.Append("&widthratio=" + widthRatio.ToString(CultureInfo.InvariantCulture));
        //        }

        //        if (ratioMode == ImageCropRatioMode.Height && width != null)
        //        {
        //            // if only width specified then assume a sqaure
        //            if (height == null)
        //            {
        //                height = width;
        //            }

        //            var heightRatio = (decimal)height / (decimal)width;
        //            imageProcessorUrl.Append("&heightratio=" + heightRatio.ToString(CultureInfo.InvariantCulture));
        //        }

        //        if (upScale == false)
        //        {
        //            imageProcessorUrl.Append("&upscale=false");
        //        }

        //        if (furtherOptions != null)
        //        {
        //            imageProcessorUrl.Append(furtherOptions);
        //        }

        //        if (cacheBusterValue != null)
        //        {
        //            imageProcessorUrl.Append("&rnd=").Append(cacheBusterValue);
        //        }

        //        return imageProcessorUrl.ToString();
        //    }

        //    return string.Empty;
        //}


    }
}
