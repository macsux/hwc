using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.NerdbankGitVersioning;
using static Nuke.Common.Tools.NerdbankGitVersioning.NerdbankGitVersioningTasks;

public partial class Build
{
    [NerdbankGitVersioning] NerdbankGitVersioning GitVersion;
    [Parameter("Determines if release branch will have pre-release tags applied to it. Default is false, meaning when cutting new version it is considered final (stable) package")]
    readonly bool IsPreRelease = false;

    Target CutReleaseBranchMajor => _ => _
        .Description("Creates a release branch with current version number, and increments Major version number for current branch. Set --is-pre-release=true to make a final release, otherwise it will have a '-beta'")
        .Executes(() => NerdbankGitVersioningPrepareRelease(_ => _
            .SetVersionIncrement("major")
            .SetProcessWorkingDirectory(RootDirectory)
            .SetTag(IsPreRelease ? "beta" : null)));
        
    Target CutReleaseBranchMinor => _ => _
        .Description("Creates a release branch with current version number, and increments Minor version number for current branch. Set --is-pre-release=true to make a final release, otherwise it will have a '-beta'")
        .Executes(() => NerdbankGitVersioningPrepareRelease(_ => _
            .SetProcessWorkingDirectory(RootDirectory)
            .SetVersionIncrement("minor")
            .SetTag(IsPreRelease ? "beta" : null)));

    
}