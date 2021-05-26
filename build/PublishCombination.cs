using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nuke.Common.ProjectModel;
using System.Linq;

namespace Nuke
{
    public record PublishCombination(Project Project, string Configuration = null, string Framework = null, [CanBeNull] string RuntimeIdentifier = null, bool? SelfContained = null)
    {
        public virtual bool Equals(PublishCombination other) =>
            other != null &&
            EqualityComparer<string>.Default.Equals(Project.Path, other.Project.Path) &&
            EqualityComparer<string>.Default.Equals(Configuration, other.Configuration) &&
            EqualityComparer<string>.Default.Equals(Framework, other.Framework) &&
            EqualityComparer<string>.Default.Equals(RuntimeIdentifier, other.RuntimeIdentifier) &&
            EqualityComparer<bool?>.Default.Equals(SelfContained, other.SelfContained);
            
        public override int GetHashCode() => HashCode.Combine(Project.Path, Configuration, Framework, RuntimeIdentifier, SelfContained);

        /// <summary>
        /// Returns all publish combinations that the project supports that have not being explicitly specified by this configuration.
        /// For example if project lists netcoreapp3.1;net5.0 for framework, and win-x64;win-x86, this method would return 4 values for each possible build permutation
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PublishCombination> Expand()
        {
            var msbuildProject = Project.GetMSBuildProject();
            
            var configurations = Configuration != null ? Configuration.SingletonList() : msbuildProject.GetPropertyValues<string>("Configurations");
            var targetFrameworks = Framework != null ? Framework.SingletonList() : msbuildProject.GetPropertyValues<string>("TargetFrameworks")
                .DefaultIfEmpty(msbuildProject.GetPropertyValue<string>("TargetFramework"));
            var runtimeIdentifiers = RuntimeIdentifier != null ? RuntimeIdentifier.SingletonList() : msbuildProject.GetPropertyValues<string>("RuntimeIdentifiers")
                .DefaultIfEmpty(msbuildProject.GetPropertyValue<string>("RuntimeIdentifier", allowImported: false))
                .ToList();
            
            IList<bool> selfContained;
            if (SelfContained.HasValue)
                selfContained = SelfContained.Value.SingletonList();
            else if(!runtimeIdentifiers.Any(x => x != null))
                selfContained = false.SingletonList();
            else
                selfContained = new[] {true, false};
                
            return from framework in targetFrameworks
                from configuration in configurations
                from rid in runtimeIdentifiers
                from sc in selfContained
                select new PublishCombination(Project, configuration, framework, rid, sc);
        }
    }
}