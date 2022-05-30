namespace API.DH2.Requests
{
    public class MeteringPointsRequest : ISoapRequest
    {
        public string Envelope { get; }
        public string Action { get; }
        private MeteringPointsRequest(string searchTerm)
        {
            Action = "listMeteringPointsCAV03";
            Envelope = @$"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:datahub:customeraccess:v03"">
                    <soapenv:Header/>
                    <soapenv:Body>
                        <urn:ListMeteringPointsCARequest>
                            {searchTerm}
                        </urn:ListMeteringPointsCARequest>
                    </soapenv:Body>
                </soapenv:Envelope>";
        }
        public static MeteringPointsRequest FromTIN(string tin)
        {
            return new MeteringPointsRequest(@$"<urn:CVRNumber>{tin}</urn:CVRNumber>");
        }

        public static MeteringPointsRequest FromSSN(string ssn)
        {
            return new MeteringPointsRequest(@$"<urn:CPRNumber>{ssn}</urn:CPRNumber>");
        }
    }
}
