using System.ComponentModel.DataAnnotations;

namespace newstrek.Models
{
    public class InterestedTopic
    {
        [Key]
        public long Id { get; set; }
        public bool? world { get; set; } = false;
        public bool? business { get; set; } = false;
        public bool? politics { get; set; } = false;
        public bool? health { get; set; } = false;
        public bool? climate { get; set; } = false;
        public bool? tech { get; set; } = false;
        public bool? entertainment { get; set; } = false;
        public bool? science { get; set; } = false;
        public bool? history { get; set; } = false;
        public bool? sports { get; set; } = false;
        public long UserId { get; set; } // Foreign key
        public User User { get; set; } // Navigation property
    }
}
