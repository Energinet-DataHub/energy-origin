using System.Reflection;
using Newtonsoft.Json;

namespace EventStore.Serialization;

public abstract class EventModel
{
    private readonly EventModelVersionAttribute _attribute;

    [JsonIgnore]
    public string Type => _attribute.Type;

    [JsonIgnore]
    public int Version => _attribute.Version;

    [JsonIgnore]
    public string Data => JsonConvert.SerializeObject(this);

    public EventModel()
    {
        Type t = GetType();

        this._attribute = t.GetCustomAttribute<EventModelVersionAttribute>(false) ??
            throw new NotSupportedException("All classes extending from EventModel must specify EventModelVersionAttribute");
    }

    public virtual EventModel? NextVersion()
    {
        return null;
    }
}
