using System.Reflection;
using Newtonsoft.Json;

namespace EventStore.Serialization;

public abstract class EventModel
{
    private EventModelVersionAttribute attribute;

    [JsonIgnore]
    public string Type => this.attribute.Type;

    [JsonIgnore]
    public int Version => this.attribute.Version;

    [JsonIgnore]
    public string Data => JsonConvert.SerializeObject(this);

    public EventModel()
    {
        Type t = GetType();

        this.attribute = t.GetCustomAttribute<EventModelVersionAttribute>(false) ??
            throw new NotSupportedException("All classes extending from EventModel must specify EventModelVersionAttribute");
    }

    public virtual EventModel? NextVersion()
    {
        return null;
    }
}
