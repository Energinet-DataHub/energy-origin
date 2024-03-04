using System.ComponentModel;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        Assert.Equal(input, ActivityLogExtensions.EntityTypeMapper(ActivityLogExtensions.EntityTypeMapper(input)));
    }

    [Fact]
    public void ActionTypeMapper_ShouldThrowInvalidEnumArgumentExceptionForUndefinedValue()
    {
        var invalidActionType = (ActivityLogEntry.ActionTypeEnum)(-1); // An undefined enum value

        Assert.Throws<InvalidEnumArgumentException>(() => ActivityLogExtensions.ActionTypeMapper(invalidActionType));
    }


    [Fact]
    public void ActorTypeMapper_ShouldThrowInvalidEnumArgumentExceptionForUndefinedValue()
    {
        var invalidActorType = (ActivityLogEntry.ActorTypeEnum)(-1);

        Assert.Throws<InvalidEnumArgumentException>(() => ActivityLogExtensions.ActorTypeMapper(invalidActorType));
    }

    [Fact]
    public void EntityTypeMapper_ShouldThrowInvalidEnumArgumentExceptionForUndefinedValue_FromActivityLogEntryToResponse()
    {
        var invalidEntityType = (ActivityLogEntry.EntityTypeEnum)(-1); // An undefined enum value

        Assert.Throws<InvalidEnumArgumentException>(() => ActivityLogExtensions.EntityTypeMapper(invalidEntityType));
    }

    [Fact]
    public void EntityTypeMapper_ShouldThrowInvalidEnumArgumentExceptionForUndefinedValue_FromResponseToActivityLogEntry()
    {
        var invalidRequestEntityType = (ActivityLogEntryResponse.EntityTypeEnum)(-1); // An undefined enum value

        Assert.Throws<InvalidEnumArgumentException>(() => ActivityLogExtensions.EntityTypeMapper(invalidRequestEntityType));
    }
}
