using System.ComponentModel.DataAnnotations;

namespace TestingIV.Models
{
    public class TransactionRequestViewModel
    {
        [Required(ErrorMessage = "partnerkey is required.")]
        [MaxLength(50, ErrorMessage = "partnerkey must not exceed 50 characters.")]
        public string PartnerKey { get; set; } 

        [Required(ErrorMessage = "partnerrefno is required.")]
        [MaxLength(50, ErrorMessage = "partnerrefno must not exceed 50 characters.")]
        public string PartnerRefNo { get; set; } 

        [Required(ErrorMessage = "partnerpassword is required.")]
        [MaxLength(50, ErrorMessage = "partnerpassword must not exceed 50 characters.")]
        public string PartnerPassword { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "totalamount must be positive.")]
        public long TotalAmount { get; set; }

        public List<ItemDetail> Items { get; set; }

        [Required(ErrorMessage = "timestamp is required.")]
        public string Timestamp { get; set; } 

        [Required(ErrorMessage = "sig is required.")]
        public string Sig { get; set; }
    }

    public class ItemDetail
    {
        [Required(ErrorMessage = "partneritemref is required for each item.")]
        [MaxLength(50, ErrorMessage = "partneritemref must not exceed 50 characters.")]
        public string PartnerItemRef { get; set; }

        [Required(ErrorMessage = "name is required for each item.")]
        [MaxLength(100, ErrorMessage = "name must not exceed 100 characters.")]
        public string Name { get; set; }

        [Range(2, 5, ErrorMessage = "qty must be greater than 1 and not exceed 5.")]
        public int Qty { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "unitprice must be positive.")]
        public long UnitPrice { get; set; } 
    }
}