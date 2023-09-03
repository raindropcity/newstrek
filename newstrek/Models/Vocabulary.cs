using System.ComponentModel.DataAnnotations;

namespace newstrek.Models
{
    public class Vocabulary
    {
        [Key]
        public long Id { get; set; }
        public string? Word { get; set; }
        public long UserId { get; set; } // Foreign key
    }
}
