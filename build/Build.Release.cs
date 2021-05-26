using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nuke;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Components.GitHub;
using Nuke.Components.Package;
using Nuke.Components.Versioning;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Interactive.InteractiveTasks;

partial class Build : IGitHub, INuget, IVersion
{
    // [Parameter] string NugetPublicFeedUrl = NugetOrgFeed;
    // [Parameter] string NugetPublicFeedApiKey;
    // [Parameter] string NugetPrivateFeedUrl;
    // [Parameter] string NugetPrivateFeedApiKey;

    //
    // Target Release => _ => _
    //     // .DependsOn(Pack, GitHubRelease)
    //     .DependsOn(Pack)
    //     .Triggers<IGitHub>(x => x.GitHubRelease)
    //     .Triggers<INuget>(x => x.NugetRelease)
    //     .OnlyWhenDynamic(() => !ConfirmOnPublish || Confirm("Are you sure you want to publish nuget artifacts?"))
    //     .Executes(() =>
    //     {
    //         var nuget = this as INuget;
    //         var github = this as IGitHub;
    //         nuget.NugetFeedsToPublish.Add(new(github.GitHubNugetPackageFeedUrl, github.GitHubToken));
    //         if (this.As<IVersion>().GitVersion.PublicRelease)
    //         {
    //             nuget.NugetFeedsToPublish.Add(new INuget.NugetFeedInfo(INuget.NugetOrgFeed, this.As<INuget>().NugetApiKey));
    //         }
    //     });

}
