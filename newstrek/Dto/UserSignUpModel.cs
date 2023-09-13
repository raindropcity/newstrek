using System.ComponentModel.DataAnnotations;

namespace newstrek.Dto
{
    public class UserSignUpModel
    {
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public InterestedTopicDto InterestedTopicDto { get; set; }
    }
}
