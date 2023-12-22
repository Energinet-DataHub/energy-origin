using System;
using ProjectOrigin.WalletSystem.V1;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace RegistryConnector.Worker.Converters;

public class ReceiveRequestConverter : JsonConverter<ReceiveRequest>
{
    public override ReceiveRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReceiveRequest.Parser.ParseFrom(reader.GetBytesFromBase64());

    public override void Write(Utf8JsonWriter writer, ReceiveRequest value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value.ToByteArray());
}
