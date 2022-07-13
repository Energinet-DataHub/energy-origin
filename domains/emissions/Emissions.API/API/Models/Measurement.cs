namespace API.Models;

//NOTE: This DTO is a reflection of the output from the DataSync API.
public class Measurement
{
    public string GSRN { get; set; }

    public long DateFrom { get; set; }

    public long DateTo { get; set; }

    public int Quantity { get; set; }

    public Quality Quality { get; set; }

    public Measurement(string gsrn, long dateFrom, long dateTo, int quantity, Quality quality)
    {
        GSRN = gsrn;
        DateFrom = dateFrom;
        DateTo = dateTo;
        Quantity = quantity;
        Quality = quality;
    }
}