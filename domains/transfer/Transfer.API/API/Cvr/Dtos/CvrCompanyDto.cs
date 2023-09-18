namespace API.Cvr.Dtos
{
    public record CvrCompanyDto
    {
        /// <summary>
        /// Company CVR number
        /// </summary>
        public required string CompanyCvr { get; init; }

        /// <summary>
        /// Company name
        /// </summary>
        public required string? CompanyName { get; init; }
    }
}
