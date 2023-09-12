using newstrek.Models;
using System.Text.Json.Serialization;

namespace newstrek.Models
{
    public class User
    {
        public long Id { get; set; }
        public string? Provider { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public InterestedTopic InterestedTopic { get; set; }
        public List<Vocabulary>? Vocabularies { get; set; }
        public string? AccessToken { get; set; }
        public long AccessExpired { get; set; }
        public DateTime? LoginAt { get; set; }
    }
}
