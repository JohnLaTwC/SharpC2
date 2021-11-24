using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpC2.Models
{
    public class Handler : Result
    {
        public string Name { get; set; }
        public IEnumerable<Parameter> Parameters { get; set; }
        public bool Running { get; set; }

        public string ParametersAsString
        {
            get
            {
                if (!Parameters.Any()) return "";

                var sb = new StringBuilder();
                
                foreach (var parameter in Parameters)
                {
                    sb.AppendLine($"{parameter.Name}: {parameter.Value} ");
                }
                
                return sb.ToString().TrimEnd(' ');
            }
        }

        public class Parameter
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool Optional { get; set; }
        }

        protected internal override IList<ResultProperty> ResultProperties => new List<ResultProperty>
        {
            new() { Name = "Name", Value = Name },
            new() { Name = "Running", Value = Running }
        };
    }
}