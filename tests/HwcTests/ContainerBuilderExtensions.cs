using System.Collections.Generic;
using System.Linq;
using Ductus.FluentDocker.Builders;

namespace HwcTests
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder WithEnvironment(this ContainerBuilder builder, Dictionary<string, object> variables) 
            => builder.WithEnvironment(variables.Select(x => $"{x.Key}={x.Value}").ToArray());
    }
}