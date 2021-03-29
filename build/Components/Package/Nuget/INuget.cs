using System;
using System.Collections.Generic;
using System.Linq;
using Ductus.FluentDocker.Model.Containers;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Nuke.Components.Project;
using Nuke.Components.Versioning;
using Nuke.Interactive;
using Nuke.NukeExtensions;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Interactive.InteractiveTasks;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Nuke.Components.Package.Nuget
{
    public interface INuget : INukeBuild, ICompileSolution //, IProvideOutput
    {
        const string NugetOrgFeed = "https://api.nuget.org/v3/index.json";
        [Parameter("Nuget ApiKey required in order to push packages")] 
        string NugetApiKey => this.Get();
        [Parameter] 
        string NugetFeed => this.Get(NugetOrgFeed);
        string NugetVersion => this.Get((this as IVersion)?.GitVersion?.NuGetPackageVersion);
        
        AbsolutePath PackageNugetOutput => this.Get(ArtifactsDirectory);
        List<AbsolutePath> NugetArtifacts { get => this.Get(); set => this.Set(value); }
        List<NugetFeedInfo> NugetFeedsToPublish { get => this.Get(new List<NugetFeedInfo>{new (NugetFeed, NugetApiKey)}); set => this.Set(value); }
        
        /// <summary>
        /// Packages projects that are Nuget Packable, moves artifacts into folder as define by <see cref="Nuke.Components.Project.ICompileSolution.ArtifactsDirectory"/> property,
        /// and outputs list of nuget packages produced into <see cref="NugetArtifacts"/> variable
        /// </summary>
        Target NugetPackage => _ => _
            .Requires(() => NugetVersion != null)
            .Executes(() =>
            {
                var temp = TemporaryDirectory / Guid.NewGuid().ToString("N");
                DotNetPack(c => c
                    .SetVersion(NugetVersion)
                    .SetProject(Solution)
                    .SetOutputDirectory(temp));
                var output = temp.GlobFiles("**");
                output.ForEach(file =>
                {
                    var destination = PackageNugetOutput / temp.GetRelativePathTo(file);
                    FileSystemTasks.MoveFile(file, destination, FileExistsPolicy.Overwrite);
                    // var currentTarget = ExecutingTargets.First(x => x.Status == ExecutionStatus.Executing).Name;
                    // ReportOutput(currentTarget, file);
                    NugetArtifacts.Add(file);
                }); 
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