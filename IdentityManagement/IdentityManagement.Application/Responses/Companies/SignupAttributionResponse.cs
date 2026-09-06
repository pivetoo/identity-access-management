namespace IdentityManagement.Application.Responses.Companies
{
    public sealed class SignupAttributionResponse
    {
        public string? Source { get; set; }

        public string? Medium { get; set; }

        public string? Campaign { get; set; }

        public string? Content { get; set; }

        public string? Term { get; set; }

        public string? Gclid { get; set; }

        public string? Fbclid { get; set; }

        public string? LandingPage { get; set; }

        public string? Referrer { get; set; }
    }
}
