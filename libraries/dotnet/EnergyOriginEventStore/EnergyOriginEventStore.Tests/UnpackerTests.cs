using EnergyOriginEventStore.Tests.Topics;
using EventStore;
using EventStore.Serialization;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class UnpackerTests
{
    [Fact]
    public void Unpacker_UnpackSaid_success()
    {
        Unpacker packer = new Unpacker();

        var message = new Said("Anton Actor", "I like to act!");
        var _event = Event.From(message);

        var unpacked_message = packer.UnpackModel(_event) as Said;

        Assert.NotNull(unpacked_message);
        Assert.Equal(message.Actor, unpacked_message?.Actor);
        Assert.Equal(message.Statement, unpacked_message?.Statement);
    }

    [Fact]
    public void Unpacker_UnpackOldUserCreate_NewestUserCreated()
    {
        Unpacker packer = new Unpacker();

        var message = new UserCreatedVersion1("123", "my-id");
        var _event = Event.From(message);

        var unpacked_message = packer.UnpackModel(_event);
        Assert.IsType<UserCreated>(unpacked_message);

        var types_message = (UserCreated)unpacked_message;

        Assert.NotNull(unpacked_message);
        Assert.Equal(message.Id, types_message.Id);
        Assert.Equal(message.Subject, types_message.Subject);
        Assert.Equal("Anonymous", types_message.NickName);
    }
}
