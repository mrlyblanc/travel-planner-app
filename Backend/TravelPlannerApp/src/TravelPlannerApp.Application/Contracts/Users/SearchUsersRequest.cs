using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Users;

public sealed class SearchUsersRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 25)]
    public int Limit { get; set; } = 10;
}
