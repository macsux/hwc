using Nuke.Common;
using Nuke.Components.GitHub;
using Nuke.Components.Package.Nuget;

namespace Nuke.Components
{
    public interface IRelease : INukeBuild
    {
        
        Target Release => _ => _
            .TryDependsOn<IGitHub>(x => x.GitHubRelease)
            .TryDependsOn<INuget>(x => x.NugetRelease);

        // .TryDependsOn<IGitHub>(x => x.GitHubRelease)
    }
}