namespace Our.Umbraco.AzureCDNToolkit.Installer
{
    using System;
    using System.Web;

    using Microsoft.Web.XmlTransform;

    using global::Umbraco.Core.Logging;
    using global::Umbraco.Core.PackageActions;
    using System.Xml.Linq;

    public class PackageActions
    {
        public class TransformConfig : IPackageAction
        {

            private readonly ILogger _logger;

            public TransformConfig(ILogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public string Alias()
            {
                return "AzureCDNToolkit.TransformConfig";
            }

            public XElement SampleXml()
            {
                return XElement.Parse("<Action runat=\"install\" undo=\"true\" alias=\"AzureCDNToolkit.TransformConfig\" file=\"~/web.config\" xdtfile=\"~/app_plugins/AzureCDNToolkit/install/web.config\"></Action>");
            }

            public bool Execute(string packageName, XElement xmlData)
            {
                return Transform(packageName, xmlData);
            }

            public bool Undo(string packageName, XElement xmlData)
            {
                return Transform(packageName, xmlData, true);
            }

            private bool Transform(string packageName, XElement xmlData, bool uninstall = false)
            {
                // The config file we want to modify
                if (xmlData.HasAttributes)
                {
                    var file = xmlData.Attribute("file").Value;

                    var sourceDocFileName = VirtualPathUtility.ToAbsolute(file);

                    // The xdt file used for tranformation
                    var fileEnd = "install.xdt";
                    if (uninstall)
                    {
                        fileEnd = string.Format("un{0}", fileEnd);
                    }

                    var xdtfile = string.Format("{0}.{1}", xmlData.Attribute("xdtfile").Value, fileEnd);
                    var xdtFileName = VirtualPathUtility.ToAbsolute(xdtfile);

                    // The translation at-hand
                    using (var xmlDoc = new XmlTransformableDocument())
                    {
                        xmlDoc.PreserveWhitespace = true;
                        xmlDoc.Load(HttpContext.Current.Server.MapPath(sourceDocFileName));

                        using (var xmlTrans = new XmlTransformation(HttpContext.Current.Server.MapPath(xdtFileName)))
                        {
                            if (xmlTrans.Apply(xmlDoc))
                            {
                                // If we made it here, sourceDoc now has transDoc's changes
                                // applied. So, we're going to save the final result off to
                                // destDoc.
                                try
                                {
                                    xmlDoc.Save(HttpContext.Current.Server.MapPath(sourceDocFileName));
                                }
                                catch (Exception e)
                                {
                                    // Log error message
                                    _logger.Error<PackageActions>(e, "Error executing TransformConfig package action (check file write permissions): {Message}", e.Message);
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
        }
    }
}
