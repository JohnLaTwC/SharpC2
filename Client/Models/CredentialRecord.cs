using System.Collections.Generic;

namespace SharpC2.Models
{
    public class CredentialRecord : Result
    {
        public string Guid { get; set; }
        public string Username { get; set; }
        public string Domain { get; set; }
        public string Password { get; set; }
        public string Source { get; set; }

        protected internal override IList<ResultProperty> ResultProperties => new List<ResultProperty>
        {
            new() { Name = "Guid", Value = Guid },
            new() { Name = "Username", Value = Username },
            new() { Name = "Domain", Value = Domain },
            new() { Name = "Password", Value = Password },
            new() { Name = "Source", Value = Source },
        };
    }
}