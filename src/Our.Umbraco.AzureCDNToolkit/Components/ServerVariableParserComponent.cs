using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Our.Umbraco.AzureCDNToolkit.Controllers;
using Umbraco.Core.Composing;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

namespace Our.Umbraco.AzureCDNToolkit.Components
{
    public class ServerVariableParserComponent : IComponent
    {
        public void Initialize()
        {
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;
        }

        public void Terminate()
        {
            ServerVariablesParser.Parsing -= ServerVariablesParser_Parsing;
        }

        void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null) return;
            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            var mainDictionary = new Dictionary<string, object>
            {
                {
                    "cacheApiBaseUrl",
                    urlHelper.GetUmbracoApiServiceBaseUrl<CacheApiController>(controller => controller.GetAllServers())
                }
            };

            if (!e.Keys.Contains("azureCdnToolkitUrls"))
            {
                e.Add("azureCdnToolkitUrls", mainDictionary);
            }
        }
    }
}
