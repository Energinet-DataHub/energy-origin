using System;

namespace API.ApiModels
{
    public class TransferAgreement
    {
        public Guid Id { get; set; }
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public Guid ProviderId { get; set; }
        public Participant Provider { get; set; }
        public Guid ReceiverId { get; set; }
        public Participant Receiver { get; set; }
    }

}
