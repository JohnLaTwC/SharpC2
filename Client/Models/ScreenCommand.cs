using System.Collections.Generic;
using System.Text;

namespace SharpC2.Models
{
    public abstract class ScreenCommand : Result
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract List<Argument> Arguments { get; }

        // [optional]
        // <required>
        public string Usage
        {
            get
            {
                var sb = new StringBuilder($"Usage: {Name} ");

                if (Arguments is not null)
                {
                    foreach (var argument in Arguments)
                    {
                        sb.Append(argument.Optional ? "[" : "<");
                        sb.Append(argument.Name);
                        sb.Append(argument.Optional ? "]" : ">");
                        sb.Append(' ');
                    }
                }

                return sb.ToString().TrimEnd(' ');
            }
        }

        public abstract Screen.Callback Execute { get; }
        
        public class Argument
        {
            public string Name { get; init; }
            public bool Artefact { get; init; }
            public bool Optional { get; init; }
        }

        protected internal override IList<ResultProperty> ResultProperties => new List<ResultProperty>
        {
            new() { Name = "Name", Value = Name },
            new() { Name = "Description", Value = Description }
        };
    }
}