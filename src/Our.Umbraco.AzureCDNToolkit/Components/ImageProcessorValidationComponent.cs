using System.Linq;
using System.Web;
using System.Web.Configuration;
using ImageProcessor.Web.HttpModules;
using Umbraco.Core.Composing;
using Umbraco.Web.Security;

namespace Our.Umbraco.AzureCDNToolkit.Components
{
    public class ImageProcessorValidationComponent : IComponent
    {
        public void Initialize()
        {
            ImageProcessingModule.ValidatingRequest += ImageProcessingModule_ValidatingRequest;
        }

        public void Terminate()
        {
            ImageProcessingModule.ValidatingRequest -= ImageProcessingModule_ValidatingRequest;
        }

        private void ImageProcessingModule_ValidatingRequest(object sender, ImageProcessor.Web.Helpers.ValidatingRequestEventArgs args)
        {
            var securityToken = WebConfigurationManager.AppSettings["AzureCDNToolkit:SecurityToken"];
            var securityModeEnabled = bool.Parse(WebConfigurationManager.AppSettings["AzureCDNToolkit:SecurityModeEnabled"]);

            if (securityModeEnabled && !string.IsNullOrWhiteSpace(args.QueryString) && !string.IsNullOrEmpty(securityToken))
            {
                var queryCollection = HttpUtility.ParseQueryString(args.QueryString);

                // if token is not present or value doesn't match then we can cancel the request
                if (!queryCollection.AllKeys.Contains("securitytoken") || queryCollection["securitytoken"] != securityToken)
                {
                    // We can allow on-demand image processor requests if the user has an Umbraco auth ticket which means they are logged into Umbraco for things like grid editor previews
                    var ticket = new HttpContextWrapper(HttpContext.Current).GetUmbracoAuthTicket();
                    if (ticket == null)
                    {
                        args.Cancel = true;
                    }
                }
            }
        }
    }
}