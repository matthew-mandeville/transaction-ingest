using System.ComponentModel.DataAnnotations;

namespace TransactionIngest.Models
{
    public class StatusType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum StatusTypeValues
    {
        Active = 1,
        Revoked = 2,
        Finalized = 3
    }
}
