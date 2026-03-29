using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TravelPlannerApp.Api.Endpoints.Requests;

public sealed class SearchUsersQuery
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 25)]
    public int Limit { get; set; } = 10;

    public static ValueTask<SearchUsersQuery?> BindAsync(HttpContext context, ParameterInfo _)
    {
        var query = context.Request.Query["query"].ToString();
        var limitText = context.Request.Query["limit"].ToString();
        var limit = string.IsNullOrWhiteSpace(limitText)
            ? 10
            : int.TryParse(limitText, out var parsedLimit)
                ? parsedLimit
                : 0;

        return ValueTask.FromResult<SearchUsersQuery?>(new SearchUsersQuery
        {
            Query = query,
            Limit = limit
        });
    }
}
