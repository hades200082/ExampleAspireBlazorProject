#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Application.Host.Api;

public class UlidRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (values.TryGetValue(routeKey, out var routeValue))
        {
            if (routeValue is string stringValue)
            {
                return Ulid.TryParse(stringValue, out _);
            }
        }
        return false;
    }
}