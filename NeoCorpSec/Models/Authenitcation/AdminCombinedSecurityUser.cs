namespace NeoCorpSec.Models.Authenitcation
{
    public class AdminCombinedSecurityUser
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string HiredDate { get; set; }
        public List<string> Roles { get; set; }

        public AdminCombinedSecurityUser()
        {
            Roles = new List<string>();
        }
    }

}
