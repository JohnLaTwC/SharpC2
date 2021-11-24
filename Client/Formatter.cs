// Author: Ryan Cobb (@cobbr_io)
// Project: SharpSploit (https://github.com/cobbr/SharpSploit)
// License: BSD 3-Clause

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace SharpC2
{
    public sealed class GenericObjectResult : Result
    {
        public object Result { get; }
        protected internal override IList<ResultProperty> ResultProperties =>
            new List<ResultProperty>
            {
                new()
                {
                    Name = Result.GetType().Name,
                    Value = Result
                }
            };

        public GenericObjectResult(object result)
        {
            Result = result;
        }
    }

    public class ResultList<T> : IList<T> where T : Result
    {
        private List<T> Results { get; } = new List<T>();

        public int Count => Results.Count;
        public bool IsReadOnly => ((IList<T>)Results).IsReadOnly;
        
        public override string ToString()
        {
            if (Results.Count <= 0) return "";
            
            var labels = new StringBuilder();
            var underlines = new StringBuilder();
            var rows = new List<StringBuilder>();
            
            for (var i = 0; i < Results.Count; i++)
            {
                rows.Add(new StringBuilder());
            }
            
            for (var i = 0; i < Results[0].ResultProperties.Count; i++)
            {
                labels.Append(Results[0].ResultProperties[i].Name);
                underlines.Append(new string('-', Results[0].ResultProperties[i].Name.Length));
                var maxproplen = 0;
                
                for (var j = 0; j < rows.Count; j++)
                {
                    var property = Results[j].ResultProperties[i];
                    var valueString = property.Value is null ? "-" : property.Value.ToString();
                    rows[j].Append(valueString);
                    
                    if (maxproplen < valueString.Length)
                        maxproplen = valueString.Length;
                }
                
                if (i != Results[0].ResultProperties.Count - 1)
                {
                    labels.Append(new string(' ', Math.Max(2, maxproplen + 2 - Results[0].ResultProperties[i].Name.Length)));
                    underlines.Append(new string(' ', Math.Max(2, maxproplen + 2 - Results[0].ResultProperties[i].Name.Length)));
                    
                    for (var j = 0; j < rows.Count; j++)
                    {
                        var property = Results[j].ResultProperties[i];
                        var valueString = property.Value is null ? "-" : property.Value.ToString();
                        rows[j].Append(new string(' ', Math.Max(Results[0].ResultProperties[i].Name.Length - valueString.Length + 2, maxproplen - valueString.Length + 2)));
                    }
                }
            }
            
            labels.AppendLine();
            labels.Append(underlines);
            
            foreach (var row in rows)
            {
                labels.AppendLine();
                labels.Append(row);
            }
            
            return labels.ToString();
        }

        public T this[int index] { get => Results[index]; set => Results[index] = value; }

        public IEnumerator<T> GetEnumerator()
        {
            return Results.Cast<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Results.Cast<T>().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Results.IndexOf(item);
        }

        public void Add(T t)
        {
            Results.Add(t);
        }

        public void AddRange(IEnumerable<T> range)
        {
            Results.AddRange(range);
        }

        public void Insert(int index, T item)
        {
            Results.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Results.RemoveAt(index);
        }

        public void Clear()
        {
            Results.Clear();
        }

        public bool Contains(T item)
        {
            return Results.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Results.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Results.Remove(item);
        }
    }

    public abstract class Result
    {
        protected internal abstract IList<ResultProperty> ResultProperties { get; }
    }

    public class ResultProperty
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}