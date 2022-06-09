namespace API.Models
{
    public class Field
    {
        public string Type { get; }

        public string Id { get; }

        public Field(string type, string id)
        {
            Type = type;
            Id = id;
        }
    }
}
