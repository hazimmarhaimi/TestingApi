namespace TestingIV.Services
{
    public interface IPartnerService
    {
        Task<Partner> GetPartnerAsync(string partnerKey);
    }

    // Define Partner as a record within the service namespace for simplicity
    public record Partner(string PartnerKey, string Password);
}