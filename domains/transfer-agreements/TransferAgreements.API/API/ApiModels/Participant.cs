using System;

namespace API.ApiModels
{
    public class Participant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long TIN { get; set; }
        public long Tin { get; internal set; }
    }
}
