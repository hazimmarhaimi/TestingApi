using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestingIV.Models;
using TestingIV.Services;
using log4net;
using Newtonsoft.Json; // Install Newtonsoft.Json via NuGet

namespace TestingIV.Controllers
{
    [ApiController]
    [Route("api")]
    public class TransactionController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ISignatureService _signatureService;
        private readonly ILog _logger;

        public TransactionController(
            IPartnerService partnerService,
            ISignatureService signatureService,
            ILog logger)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
            _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("submittrxmessage")]
        public async Task<IActionResult> SubmitTrxMessage([FromBody] TransactionRequestViewModel request)
        {
            
            string requestJson = JsonConvert.SerializeObject(request, Formatting.Indented);
            _logger.Info($"Received request: {requestJson}");

            
            if (!ModelState.IsValid)
            {
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

           
            if (!TryParseTimestamp(request.Timestamp, out DateTime requestTime))
            {
                ModelState.AddModelError("Timestamp", "Invalid timestamp format.");
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

            if (!IsTimestampValid(requestTime, DateTime.UtcNow))
            {
                ModelState.AddModelError("Timestamp", "Expired.");
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

            
            var partner = await _partnerService.GetPartnerAsync(request.PartnerKey);
            if (partner == null)
            {
                ModelState.AddModelError("PartnerKey", "Invalid or unauthorized partner.");
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

            if (!IsPasswordValid(request.PartnerPassword, partner.Password))
            {
                ModelState.AddModelError("PartnerPassword", "Invalid partner password.");
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

            string computedSig = _signatureService.ComputeSignature(
                requestTime,
                request.PartnerKey,
                request.PartnerRefNo,
                request.TotalAmount,
                request.PartnerPassword);

            if (computedSig != request.Sig.Trim())
            {
                ModelState.AddModelError("Sig", "Signature mismatch.");
                var response = CreateErrorResponse();
                LogResponse(response);
                return BadRequest(response);
            }

            
            if (request.Items?.Count > 0)
            {
                var itemValidationResult = ValidateItems(request.Items, request.TotalAmount);
                if (!itemValidationResult.IsValid)
                {
                    ModelState.AddModelError("Items", itemValidationResult.ErrorMessage);
                    var response = CreateErrorResponse();
                    LogResponse(response);
                    return BadRequest(response);
                }
            }

            
            (long totalDiscount, long finalAmount) = CalculateDiscount(request.TotalAmount);
            var successResponse = new TransactionResponseViewModel
            {
                Result = 1,
                ResultMessage = "Success",
                TotalAmount = request.TotalAmount,
                TotalDiscount = totalDiscount,
                FinalAmount = finalAmount
            };

            LogResponse(successResponse);
            return Ok(successResponse);
        }

        #region Helper Methods

        private static bool TryParseTimestamp(string timestamp, out DateTime requestTime)
        {
            try
            {
                requestTime = DateTime.Parse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
                return requestTime.Kind == DateTimeKind.Utc;
            } catch (FormatException)
            {
                requestTime = default;
                return false;
            }
        }

        private static bool IsTimestampValid(DateTime requestTime, DateTime serverTime)
        {
            TimeSpan timeDifference = serverTime - requestTime;
            return Math.Abs(timeDifference.TotalMinutes) <= 5;
        }

        private static bool IsPasswordValid(string encodedPassword, string storedPassword)
        {
            try
            {
                byte[] passwordBytes = Convert.FromBase64String(encodedPassword);
                string decodedPassword = Encoding.UTF8.GetString(passwordBytes);
                return decodedPassword == storedPassword;
            } catch (FormatException)
            {
                return false;
            }
        }

        private static (bool IsValid, string ErrorMessage) ValidateItems(List<ItemDetail> items, long totalAmount)
        {
            long calculatedTotal = 0;
            foreach (var item in items)
            {
                calculatedTotal += item.Qty * item.UnitPrice;
            }

            return calculatedTotal == totalAmount ? (true, null) : (false, "Invalid Total Amount.");
        }

        private TransactionResponseViewModel CreateErrorResponse()
        {
            var errorMessage = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";
            return new TransactionResponseViewModel { Result = 0, ResultMessage = errorMessage };
        }

        private static (long TotalDiscount, long FinalAmount) CalculateDiscount(long totalAmount)
        {
            double totalAmountInMyr = totalAmount / 100.0;

            double baseDiscountPercentage = totalAmountInMyr switch
            {
                < 200 => 0,
                >= 200 and <= 500 => 5,
                >= 501 and <= 800 => 7,
                >= 801 and <= 1200 => 10,
                > 1200 => 15
            };

            double conditionalDiscountPercentage = 0;
            if (IsPrime((long)totalAmountInMyr) && totalAmountInMyr > 500)
            {
                conditionalDiscountPercentage += 8;
            }
            if (totalAmountInMyr > 900 && totalAmountInMyr % 10 == 5)
            {
                conditionalDiscountPercentage += 10;
            }

            double totalDiscountPercentage = baseDiscountPercentage + conditionalDiscountPercentage;
            if (totalDiscountPercentage > 20)
            {
                totalDiscountPercentage = 20;
            }

            long totalDiscount = (long)(totalAmount * (totalDiscountPercentage / 100));
            long finalAmount = totalAmount - totalDiscount;

            return (totalDiscount, finalAmount);
        }

        private static bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            for (long i = 3; i <= Math.Sqrt(number); i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        private void LogResponse(TransactionResponseViewModel response)
        {
            string responseJson = JsonConvert.SerializeObject(response, Formatting.Indented);
            _logger.Info($"Response sent: {responseJson}");
        }

        #endregion
    }
}