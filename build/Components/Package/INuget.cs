using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Nuke.Components.Build;
using Nuke.Components.Versioning;
using Nuke.NukeExtensions;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable SuspiciousTypeConversion.Global

namespace Nuke.Components.Package
{
    public interface INuget : INukeBuild, IBuildSolution //, IProvideOutput
    {
        const string NugetOrgFeed = "https://api.nuget.org/v3/index.json";
        [Parameter("Nuget ApiKey required in order to push packages")] 
        string NugetApiKey => this.Get();
        [Parameter] 
        string NugetFeed => this.Get(NugetOrgFeed);
        string NugetVersion => this.Get((this as IVersion)?.GitVersion?.NuGetPackageVersion);
        IList<AbsolutePath> NugetArtifacts => this.Get();
        IList<NugetFeedInfo> NugetFeedsToPublish => this.Get(new NugetFeedInfo(NugetFeed, NugetApiKey).SingletonList());
        IList<AbsolutePath> NugetProjectsToPackage => this.Get(Solution.Path.SingletonList());
        AbsolutePath NugetPackageFolder => ArtifactsDirectory / "package" / "nuget";
        
        /// <summary>
        /// Packages projects that are Nuget Packable, moves artifacts into folder as define by <see cref="IBuildSolution.ArtifactsDirectory"/> property,
        /// and outputs list of nuget packages produced into <see cref="NugetArtifacts"/> variable
        /// </summary>
        Target NugetPackage => _ => _
            .Requires(() => NugetVersion != null)
            .ExecutesToFolder(() => NugetPackageFolder, () => NugetArtifacts, 
                (outputDirectory) =>
            {
                DotNetPack(c => c
                    .SetVersion(NugetVersion)
                    .SetOutputDirectory(outputDirectory)
                    .CombineWith(NugetProjectsToPackage, (_,project) => _
                        .SetProject(project)));
            });
 
    
        /// <summary>
        /// Publishes to Nuget feeds as specified by <see cref="NugetFeedsToPublish"/> property. This defaults to single feed as provided defined by
        /// <see cref="NugetFeed"/> and <see cref="NugetApiKey"/> parameters
        /// </summary>
        Target NugetRelease => _ => _
            .DependsOn(NugetPackage)
            .Requires(() => NugetApiKey)
            // .OnlyWhenDynamic(() => !(this is IConfirm) || !((IConfirm)this).ConfirmOnPublish || NugetFeed != NugetOrgFeed  || Confirm("Are you sure you want to publish nuget artifacts?"))
            .Executes(() =>
            {
                foreach (var (feed, apiKey) in NugetFeedsToPublish)
                {
                    PublishToNugetFeed(feed, apiKey);
                }
            });

        void PublishToNugetFeed(string feedUrl, string apiKey)
        {
            // var nugetArtifacts = Items.Values.SelectMany(x => x).Where(x => x.ToString().EndsWith(".nupkg")).ToList();
            DotNetNuGetPush(_ => _
                .SetSource(feedUrl)
                .SetApiKey(apiKey)
                .CombineWith(NugetArtifacts, (oo, package) => oo
                    .SetTargetPath(package)));
        }

        public record NugetFeedInfo(string Url, string ApiKey);
    }
}