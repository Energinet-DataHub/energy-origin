namespace API.Query.API.ApiModels.Responses;

public class Technology
{
    public string AibFuelCode { get; init; }
    public string AibTechCode { get; init; }

    public Technology(string AibFuelCode, string AibTechCode)
    {
        this.AibFuelCode = AibFuelCode;
        this.AibTechCode = AibTechCode;
    }
    
    public static Technology? From(DataContext.ValueObjects.Technology? technology) => technology == null ? null : new(technology.FuelCode, technology.TechCode);
};
