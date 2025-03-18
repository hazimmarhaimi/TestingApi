using System;
using System.Security.Cryptography;
using System.Text;

namespace TestingIV.Services
{
    public class SignatureService : ISignatureService
    {
        public string ComputeSignature(DateTime timestamp, string partnerKey, string partnerRefNo, long totalAmount, string partnerPassword)
        {
            string sigTimestamp = timestamp.ToString("yyyyMMddHHmmss");
            string concatString = $"{sigTimestamp}{partnerKey}{partnerRefNo}{totalAmount}{partnerPassword}";
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatString));
            return Convert.ToBase64String(hashBytes);
        }
    }
}