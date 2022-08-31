namespace API.Emissions.Models;

public record EmissionRecord(long DateFrom, long DateTo, Quantity Total, Quantity Relative);
