using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Staff;

namespace LMS.Tests.Guests;

public class PhoneMaskTests
{
    [Fact]
    public void MasksMiddleDigitsKeepingHeadAndTail()
    {
        Assert.Equal("+853 6***4829", PhoneMask.Mask("+853 6123 4829"));
        Assert.Equal("+852 6***2187", PhoneMask.Mask("+852 6123 2187"));
    }

    [Fact]
    public void ShortNumbersAreFullyMasked()
    {
        Assert.Equal("***", PhoneMask.Mask("12345"));
        Assert.Equal("***", PhoneMask.Mask("12"));
    }

    [Fact]
    public void MissingPhoneStaysMissing()
    {
        Assert.Null(PhoneMask.Mask(null));
        Assert.Null(PhoneMask.Mask("  "));
    }
}

public class TierCodesTests
{
    [Fact]
    public void RanksAscendingTiers()
    {
        Assert.True(TierCodes.RankOf("SILVER") < TierCodes.RankOf("GOLD"));
        Assert.True(TierCodes.RankOf("DIAMOND") < TierCodes.RankOf("VIP"));
        Assert.Equal(0, TierCodes.RankOf("silver"));
    }

    [Fact]
    public void UnknownTierIsNotRanked()
    {
        Assert.Equal(-1, TierCodes.RankOf("BRONZE"));
    }
}

public class StaffPreferencesTests
{
    [Fact]
    public void AcceptsWellFormedPreferences()
    {
        var issues = StaffPreferences.Validate(
            "FD-03",
            StaffPreferences.AllNotificationTypes,
            ["location_1"],
            ["location_2"]);

        Assert.Empty(issues);
    }

    [Fact]
    public void RejectsBlankStation()
    {
        var issues = StaffPreferences.Validate(string.Empty, StaffPreferences.AllNotificationTypes, [], []);

        Assert.Contains(issues, issue => issue.Field == "station");
    }

    [Fact]
    public void RejectsUnknownAndDuplicateNotificationTypes()
    {
        var unknown = StaffPreferences.Validate("FD-03", ["BOOKING_CONFIRMED", "NOT_A_TYPE"], [], []);
        var duplicate = StaffPreferences.Validate("FD-03", ["BOOKING_CONFIRMED", "BOOKING_CONFIRMED"], [], []);

        Assert.Contains(unknown, issue => issue.Field == "enabledNotificationTypes");
        Assert.Contains(duplicate, issue => issue.Field == "enabledNotificationTypes");
    }

    [Fact]
    public void RejectsDuplicatesAndOverflowsInLocationLists()
    {
        var favorites = Enumerable.Range(0, StaffPreferences.MaxFavorites + 1).Select(index => $"location_{index}").ToList();
        var issues = StaffPreferences.Validate("FD-03", StaffPreferences.AllNotificationTypes, favorites, []);
        Assert.Contains(issues, issue => issue.Field == "favoriteLocationIds");

        var duplicatedRecent = StaffPreferences.Validate("FD-03", StaffPreferences.AllNotificationTypes, [], ["location_1", "location_1"]);
        Assert.Contains(duplicatedRecent, issue => issue.Field == "recentLocationIds");
    }
}
