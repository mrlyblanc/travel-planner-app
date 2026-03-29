namespace TravelPlannerApp.Api.Common.Concurrency;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireIfMatchHeaderAttribute : Attribute
{
}
