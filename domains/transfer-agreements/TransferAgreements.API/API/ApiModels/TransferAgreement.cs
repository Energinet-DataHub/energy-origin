using System;
using System.ComponentModel.DataAnnotations;

namespace API.ApiModels {

    public class TransferAgreement
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public long StartDate { get; set; }

        [Required]
        public long EndDate { get; set; }

        [Required]
        public long SenderTin { get; set; }

        public string? SenderName { get; set; }

        public Subject? Sender { get; set; }

        [Required]
        public int ReceiverTin { get; set; }

        public string? ReceiverName { get; set; }

        public Subject? Receiver { get; set; }
    }
}
