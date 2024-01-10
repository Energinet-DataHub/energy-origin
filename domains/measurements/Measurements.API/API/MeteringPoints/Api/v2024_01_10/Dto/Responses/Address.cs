namespace API.MeteringPoints.Api.v2024_01_10.Dto.Responses;

public class Address
{
    public Address(string address1, string? address2, string? locality, string city, string postalCode, string country)
    {
        Address1 = address1;
        Address2 = address2;
        Locality = locality;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public string Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Locality { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}
