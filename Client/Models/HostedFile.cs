using System.Collections.Generic;

namespace SharpC2.Models
{
    public class HostedFile : Result
    {
        public string Filename { get; set; }
        public long Size { get; set; }

        protected internal override IList<ResultProperty> ResultProperties => new List<ResultProperty>
        {
            new() { Name = "Filename", Value = Filename },
            new() { Name = "Size", Value = Size }
        };
    }
}