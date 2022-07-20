using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;
using EnergyOriginEventStore.Tests.Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class UnpackerTests
{
    [Fact]
    public void Unpacker_UnpackSaid_success()
    {
        Unpacker packer = new();

        var message = new Said("Anton Actor", "I like to act!");
        var @event = InternalEvent.From(message);

        var unpacked_message = packer.UnpackModel(@event) as Said;

        Assert.NotNull(unpacked_message);
        Assert.Equal(message.Actor, unpacked_message?.Actor);
        Assert.Equal(message.Statement, unpacked_message?.Statement);
    }

    [Fact]
    public void Unpacker_UnpackOldUserCreate_NewestUserCreated()
    {
        Unpacker packer = new();

        var message = new UserCreatedVersion1("123", "my-id");
        var @event = InternalEvent.From(message);

        var unpackedMessage = packer.UnpackModel(@event);
        Assert.IsType<UserCreatedVersion2>(unpackedMessage);

        var typedMessage = (UserCreatedVersion2)unpackedMessage;

        Assert.NotNull(unpackedMessage);
        Assert.Equal(message.Id, typedMessage.Id);
        Assert.Equal(message.Subject, typedMessage.Subject);
        Assert.Equal("Anonymous", typedMessage.NickName);
    }
}
