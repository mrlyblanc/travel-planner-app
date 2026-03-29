namespace TravelPlannerApp.Api.Common.Versioning;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireApiVersionHeaderAttribute : Attribute
{
}
