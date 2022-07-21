using System.Reflection;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Serialization;

public abstract record EventModel
{
    private readonly EventModelVersionAttribute _attribute;

    [JsonIgnore]
    public string Type => _attribute.Type;

    [JsonIgnore]
    public int Version => _attribute.Version;

    [JsonIgnore]
    public string Data => JsonConvert.SerializeObject(this);

    protected EventModel()
    {
        _attribute = GetType().GetCustomAttribute<EventModelVersionAttribute>(false) ??
                     throw new NotSupportedException("All classes extending from EventModel must specify EventModelVersionAttribute");
    }

    public virtual EventModel? NextVersion()
    {
        return null;
    }
}
