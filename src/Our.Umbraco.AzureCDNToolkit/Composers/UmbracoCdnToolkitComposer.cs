using Our.Umbraco.AzureCDNToolkit.Components;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.PropertyEditors.ValueConverters;

namespace Our.Umbraco.AzureCDNToolkit.Composers
{
    public class UmbracoCdnToolkitComposer : IComposer
    {

        public void Compose(Composition composition)
        {

            composition.PropertyValueConverters().Remove<RteMacroRenderingValueConverter>();

            composition.Register<DistributedCacheRefresherComponent>();
            composition.Register<ImageProcessorValidationComponent>();
            composition.Register<ServerVariableParserComponent>();

        }
    }
}
