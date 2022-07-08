using System;
using Newtonsoft.Json;
using EventStore;
using EventStore.Serialization;

// FIXME: This namespace is hard to place

namespace Topics;

public class Unpack {
    public static Event Event(string payload) => JsonConvert.DeserializeObject<Event>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");

    public static T Message<T>(Event package) where T : EventModel {
        switch (package.ModelType) {
            case "Said":
                return UnpackSaid(package.ModelVersion, package.Data) as T ?? throw new Exception($"Model type does not : {package.ModelType}");
            default:
                throw new Exception($"Message with unknown type: {package.ModelType}");
        }
    }

    static Said UnpackSaid(int version, string payload) {
        switch (version) {
            case 1:
                return JsonConvert.DeserializeObject<Said>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");
            default:
                throw new Exception($"Found Said with unknown version: {version}");
        }
    }

    static UserCreated UnpackUserCreated(int version, string payload) {
        switch (version) {
            case 2:
                return JsonConvert.DeserializeObject<UserCreated>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");
            case 1:
                var item = JsonConvert.DeserializeObject<UserCreatedVersion1>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");
                return new UserCreated(item.Id, item.Subject, "Anonymous");
            default:
                throw new Exception($"Found Said with unknown version: {version}");
        }
    }
}
