namespace API.Cvr.Dtos
{
    public record CvrCompanyDto
    {
        /// <summary>
        /// Company TIN (e.g. CVR)
        /// </summary>
        public required string CompanyTin { get; init; }

        /// <summary>
        /// Company name
        /// </summary>
        public required string? CompanyName { get; init; }
    }
}
