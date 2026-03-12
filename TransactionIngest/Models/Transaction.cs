
using System.ComponentModel.DataAnnotations;

namespace TransactionIngest.Models
{
    public class Transaction
    {
        public Transaction()
        {
            StatusTypeId = (int)StatusTypeValues.Active;
        }
        
        public int Id { get; set; }

        [Required]
        [MaxLength(19)]
        public string CardNumber { get; set; }

        [Required]
        [MaxLength(19)]
        public string LocationCode { get; set; }

        [Required]
        [MaxLength(19)]
        public string ProductName { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public int StatusTypeId { get; set; }

        [Required]
        public DateTime TimeStamp { get; set; }
    }
}
