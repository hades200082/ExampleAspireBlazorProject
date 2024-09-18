using Humanizer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Serilog;

namespace Application.Host.Api;

internal sealed class RouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var standardisedName = controller.ControllerName
            .Kebaberize()
            .Pluralize(false)
            .ToLowerInvariant();
        
        // Apply the route
        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel?.Template != null)
            {
                // Replace the [controller] placeholder with the standardized name
                selector.AttributeRouteModel.Template = 
                    selector.AttributeRouteModel.Template.Replace("[controller]", standardisedName);
            }
            else
            {
                selector.AttributeRouteModel = new AttributeRouteModel
                {
                    Template = standardisedName
                };
            }
        }
        Log.Information("Controller: {Controller}, Route: {Route}", controller.ControllerName, standardisedName);
    }
}