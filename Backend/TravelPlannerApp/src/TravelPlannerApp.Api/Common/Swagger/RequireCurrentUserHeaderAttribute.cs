namespace TravelPlannerApp.Api.Common.Swagger;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireCurrentUserHeaderAttribute : Attribute
{
}
