namespace API.Models;

public class Measurement
{
    public string GSRN { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public int Quantity { get; set; }

    public Quality Quality { get; set; }

    public Measurement(string gsrn, DateTime dateFrom, DateTime dateTo, int quantity, Quality quality)
    {
        GSRN = gsrn;
        DateFrom = dateFrom;
        DateTo = dateTo;
        Quantity = quantity;
        Quality = quality;
    }
}