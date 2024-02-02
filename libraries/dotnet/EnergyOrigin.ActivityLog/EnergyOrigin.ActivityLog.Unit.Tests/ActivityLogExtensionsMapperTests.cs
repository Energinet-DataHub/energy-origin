using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class ActivityLogExtensionsMapperTests
{
    [Theory]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Created, ActivityLogEntryResponse.ActionTypeEnum.Created)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Accepted, ActivityLogEntryResponse.ActionTypeEnum.Accepted)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Declined, ActivityLogEntryResponse.ActionTypeEnum.Declined)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Activated, ActivityLogEntryResponse.ActionTypeEnum.Activated)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Deactivated, ActivityLogEntryResponse.ActionTypeEnum.Deactivated)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.EndDateChanged, ActivityLogEntryResponse.ActionTypeEnum.EndDateChanged)]
    [InlineData(ActivityLogEntry.ActionTypeEnum.Expired, ActivityLogEntryResponse.ActionTypeEnum.Expired)]
    public void ActionTypeMapper_ShouldReturnCorrectEnum(ActivityLogEntry.ActionTypeEnum input, ActivityLogEntryResponse.ActionTypeEnum expected)
    {
        var result = ActivityLogExtensions.ActionTypeMapper(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ActivityLogEntry.ActorTypeEnum.User, ActivityLogEntryResponse.ActorTypeEnum.User)]
    [InlineData(ActivityLogEntry.ActorTypeEnum.System, ActivityLogEntryResponse.ActorTypeEnum.System)]
    public void ActorTypeMapper_ShouldReturnCorrectEnum(ActivityLogEntry.ActorTypeEnum input, ActivityLogEntryResponse.ActorTypeEnum expected)
    {
        var result = ActivityLogExtensions.ActorTypeMapper(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ActivityLogEntry.EntityTypeEnum.TransferAgreement, ActivityLogEntryResponse.EntityTypeEnum.TransferAgreement)]
    [InlineData(ActivityLogEntry.EntityTypeEnum.MeteringPoint, ActivityLogEntryResponse.EntityTypeEnum.MeteringPoint)]
    [InlineData(ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal, ActivityLogEntryResponse.EntityTypeEnum.TransferAgreementProposal)]
    public void EntityTypeMapper_ShouldReturnCorrectEnum(ActivityLogEntry.EntityTypeEnum input, ActivityLogEntryResponse.EntityTypeEnum expected)
    {
        var result = ActivityLogExtensions.EntityTypeMapper(input);
        Assert.Equal(expected, result);
    }
}
