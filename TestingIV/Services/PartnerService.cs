using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestingIV.Services
{
    public class PartnerService : IPartnerService
    {
        // In-memory partner data; replace with a database in production
        private static readonly Dictionary<string, string> AllowedPartners = new()
            {
                { "FAKEGOOGLE", "FAKEPASSWORD1234" },
                { "FAKEPEOPLE", "FAKEPASSWORD4578" }
            };

        public Task<Partner?> GetPartnerAsync(string partnerKey)
        {
            return Task.FromResult(AllowedPartners.TryGetValue(partnerKey, out var password)
                ? new Partner(partnerKey, password)
                : null);
        }
    }
}