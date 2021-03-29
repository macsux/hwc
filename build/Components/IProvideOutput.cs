using System.Collections.Concurrent;
using System.Collections.Generic;
using Nuke.Common.IO;

namespace Nuke.Components
{
    public interface IProvideOutput
    {
        public static readonly ConcurrentDictionary<string, List<AbsolutePath>> Items = new();

        public void ReportOutput(string groupName, params AbsolutePath[] items) => ReportOutput(groupName, (IEnumerable<AbsolutePath>) items);
        
        public void ReportOutput(string groupName, IEnumerable<AbsolutePath> items)
        {
            var group = Items.GetOrAdd(groupName, key => new());
            group.AddRange(items);
        }
    }
}