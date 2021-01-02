using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.Specialized;

using HtmlAgilityPack;

using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.PropertyEditors.ValueConverters;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.AzureCDNToolkit.ValueConverters
{

    public class RteValueConverter : RteMacroRenderingValueConverter
    {

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public RteValueConverter(
            IUmbracoContextAccessor umbracoContextAccessor,
            global::Umbraco.Web.Macros.IMacroRenderer macroRenderer,
            global::Umbraco.Web.Templates.HtmlLocalLinkParser linkParser,
            global::Umbraco.Web.Templates.HtmlUrlParser urlParser,
            global::Umbraco.Web.Templates.HtmlImageSourceParser imageSourceParser)
                : base(umbracoContextAccessor, macroRenderer, linkParser, urlParser, imageSourceParser)
        {
            _umbracoContextAccessor = umbracoContextAccessor ?? throw new ArgumentNullException(nameof(umbracoContextAccessor));
        }

        public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
        {
            // .Content in v7 is .Element in v8
            // https://our.umbraco.com/apidocs/v7/csharp/api/Umbraco.Core.PropertyEditors.PropertyCacheLevel.html
            // https://our.umbraco.com/apidocs/v8/csharp/api/Umbraco.Core.PropertyEditors.PropertyCacheLevel.html
            return PropertyCacheLevel.Element; 
        }

        public override System.Type GetPropertyValueType(IPublishedPropertyType propertyType)
        {
            return typeof(IHtmlString);
        }

        public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
        {
            if (source == null)
            {
                return null;
            }

            var coreConversion = base.ConvertDataToSource(
            propertyType,
            source,
            preview);

            // If toolkit is disabled then return base conversion
            if (!AzureCdnToolkit.Instance.UseAzureCdnToolkit)
            {
                return coreConversion;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(coreConversion.ToString());

            if (doc.ParseErrors.Any() || doc.DocumentNode == null)
            {
                return coreConversion;
            }

            var modified = false;

            ResolveUrlsForElement(doc, "img", "src", "data-id", false, false, ref modified);
            ResolveUrlsForElement(doc, "a", "href", "data-id", true, true, ref modified);

            return modified ? doc.DocumentNode.OuterHtml : coreConversion;
        }

        private void ResolveUrlsForElement(HtmlDocument doc, string elementName, string attributeName, string idAttributeName, bool idAttributeMandatory, bool asset, ref bool modified)
        {
            var htmlNodes = doc.DocumentNode.SelectNodes(string.Concat("//", elementName));

            if (htmlNodes == null)
            {
                return;
            }

            foreach (var htmlNode in htmlNodes)
            {
                var urlAttr = htmlNode.Attributes.FirstOrDefault(x => x.Name == attributeName);
                var idAttr = htmlNode.Attributes.FirstOrDefault(x => x.Name == idAttributeName);

                if (urlAttr == null || (idAttributeMandatory && idAttr == null))
                {
                    continue;
                }

                // html decode the url as variables encoded in tinymce
                var src = HttpUtility.HtmlDecode(urlAttr.Value);
                var resolvedSrc = string.Empty;

                var hasQueryString = src.InvariantContains("?");
                var querystring = new NameValueCollection();

                if (hasQueryString && src != null)
                {
                    querystring = HttpUtility.ParseQueryString(src.Substring(src.IndexOf('?')));
                }


                // can only resolve ImageProcessor Azure Cache Urls if resolvable domain is set
                if (AzureCdnToolkit.Instance.Domain == null)
                {
                    continue;
                }
                if (idAttr != null)
                {
                    // Umbraco media
                    int nodeId;
                    if (int.TryParse(idAttr.Value, out nodeId))
                    {
                        var node = _umbracoContextAccessor.UmbracoContext.Media.GetById(nodeId);

                        if (node != null)
                        {
                            if (hasQueryString)
                            {
                                resolvedSrc =
                                    new UrlHelper().ResolveCdnFallback(node, asset: asset,
                                        querystring: querystring.ToString(), fallbackImage: src).ToString();
                            }
                            else
                            {
                                resolvedSrc =
                                    new UrlHelper().ResolveCdnFallback(node, asset: asset, fallbackImage: src)
                                        .ToString();
                            }
                        }
                    }
                }
                else
                {
                    // Image in TinyMce doesn't have a data-id attribute so lets add package cache buster
                    resolvedSrc = new UrlHelper().ResolveCdn(src, asset: asset).ToString();
                }

                // If the resolved url is different to the orginal change the src attribute
                if (string.IsNullOrWhiteSpace(resolvedSrc) || resolvedSrc == string.Concat(AzureCdnToolkit.Instance.Domain, src))
                {
                    continue;
                }

                urlAttr.Value = resolvedSrc;
                modified = true;
            }
        }
    }
}