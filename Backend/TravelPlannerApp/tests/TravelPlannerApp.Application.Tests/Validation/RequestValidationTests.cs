using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Tests.Support;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Tests.Validation;

public sealed class RequestValidationTests
{
    [Fact]
    public void ItineraryRequest_WhenEndDateBeforeStartDate_ReturnsValidationError()
    {
        var results = ValidationTestHelper.Validate(new CreateItineraryRequest
        {
            Title = "Trip",
            Destination = "Tokyo",
            StartDate = new DateOnly(2026, 4, 18),
            EndDate = new DateOnly(2026, 4, 17)
        });

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateItineraryRequest.EndDate)));
    }

    [Fact]
    public void EventRequest_WhenEndDateTimeIsNotAfterStartDateTime_ReturnsValidationError()
    {
        var results = ValidationTestHelper.Validate(new CreateEventRequest
        {
            Title = "Dinner",
            Category = EventCategory.Restaurant,
            StartDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            EndDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            Timezone = "Asia/Tokyo"
        });

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateEventRequest.EndDateTime)));
    }

    [Fact]
    public void ReplaceMembersRequest_WhenEmpty_ReturnsValidationError()
    {
        var results = ValidationTestHelper.Validate(new ReplaceItineraryMembersRequest());

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ReplaceItineraryMembersRequest.UserIds)));
    }

    [Fact]
    public void ReplaceMembersRequest_WhenContainsBlankUserId_ReturnsValidationError()
    {
        var results = ValidationTestHelper.Validate(new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-ava", " "]
        });

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(ReplaceItineraryMembersRequest.UserIds)));
    }
}
