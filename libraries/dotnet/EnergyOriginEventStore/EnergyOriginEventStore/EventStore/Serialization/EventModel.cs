using System.Reflection;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Serialization;

public abstract record EventModel
{
    private readonly EventModelVersionAttribute attribute;

    [JsonIgnore]
    public string Type => attribute.Type;

    [JsonIgnore]
    public int Version => attribute.Version;

    [JsonIgnore]
    public string Data => JsonConvert.SerializeObject(this);

    protected EventModel() => attribute = GetType().GetCustomAttribute<EventModelVersionAttribute>(false) ?? throw new NotSupportedException("All classes extending from EventModel must specify EventModelVersionAttribute");

    public virtual EventModel? NextVersion() => null;
}
