using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.ValueInjection;
using Nuke.Components.Git;
using Nuke.NukeExtensions;
using static Nuke.Common.Tools.NerdbankGitVersioning.NerdbankGitVersioningTasks;
using static Nuke.Common.IO.FileSystemTasks;
namespace Nuke.Components.Versioning
{
    public interface IVersion : INukeBuild, IGit
    {
        [NerdbankGitVersioning] NerdbankGitVersioning GitVersion => this.Get();
        
        [Parameter("Determines if release branch will have pre-release tags applied to it. Default is false, meaning when cutting new version it is considered final (stable) package")]
        bool IsPreRelease
        {
            get => this.Get();
            set => this.Set(value);
        }
        
        Target SetupGitVersion => _ => _
            .Unlisted()
            .DependsOn(SetupGit)
            .Before(EnsureCommit)
            .Triggers(EnsureCommit)
            .OnlyWhenDynamic(
                () => IsGitInitialized(),
                () => !FileExists(RootDirectory / "version.json"))
            .Executes(() => NerdbankGitVersioningInstall());

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
}