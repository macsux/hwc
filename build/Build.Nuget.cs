using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Interactive.InteractiveTasks;

partial class Build
{
    const string NugetOrgFeed = "https://api.nuget.org/v3/index.json";
    [Parameter("Nuget ApiKey required in order to push packages")] string NugetApiKey;
    [Parameter] string NugetFeed = NugetOrgFeed;
    
    Target NugetRelease => _ => _
        .DependsOn(Publish)
        .Requires(() => NugetApiKey)
        .OnlyWhenDynamic(() => !ConfirmOnPublish || NugetFeed != NugetOrgFeed || Confirm("Are you sure you want to publish nuget artifacts?"))
        .Executes(() => PublishToNugetFeed(NugetFeed, NugetApiKey));

    void PublishToNugetFeed(string feedUrl, string apiKey)
    {
        var nugetArtifacts = GetPackArtifacts().Where(x => x.ToString().EndsWith(".nupkg")).ToList();
        DotNetNuGetPush(_ => _
            .SetSource(feedUrl)
            .SetApiKey(apiKey)
            .CombineWith(nugetArtifacts, (oo, package) => oo
                .SetTargetPath(package)));
    }
}
