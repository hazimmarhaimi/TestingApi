namespace TestingIV.Services
{
    public interface ISignatureService
    {
        string ComputeSignature(DateTime timestamp, string partnerKey, string partnerRefNo, long totalAmount, string partnerPassword);
    }
}