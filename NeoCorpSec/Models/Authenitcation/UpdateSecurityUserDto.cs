namespace NeoCorpSec.Models.Authenitcation
{
    public class UpdateSecurityUserDto
    {
        public string Id { get; set; }
        public string? UserName { get; set; }
        public string? SecurityUsername { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? HiredDate { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
