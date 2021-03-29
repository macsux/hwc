using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using LibGit2Sharp;
using Nuke.NukeExtensions;

namespace Nuke.Components.Git
{
    public interface IGit : INukeBuild
    {
        [GitRepositoryExt] Repository GitRepository
        {
            get => this.Get();
            set => this.Set(value);
        }
        static bool IsGitInitialized() => Repository.IsValid(NukeBuild.RootDirectory);

        bool IsCurrentBranchCommitted() => GitRepository.RetrieveStatus().IsDirty;
        bool IsRemoteOriginConfigured()
        {
            if (!IsGitInitialized())
                return false;
            using var repo = new Repository(RootDirectory);
            var origin = repo.Network.Remotes.FirstOrDefault(x => x.Name == "origin");
            return origin != null;
        }
        
        Target EnsureCommit => _ => _
            .Unlisted()
            .DependsOn(SetupGit)
            .OnlyWhenDynamic(
                () => Repository.IsValid(RootDirectory),
                () => !GitRepository.Commits.Any())
            .Executes(() =>
            {
                var signature = GitRepository.Config.BuildSignature(DateTimeOffset.UtcNow);
                Commands.Stage(GitRepository, "*");
                GitRepository.Commit("Initial", signature, signature);
            });
        
        Target SetupGit => _ => _
            .Unlisted()
            .OnlyWhenDynamic(() => !IsGitInitialized() )
            .Executes(() =>
            {
            
                Repository.Init(RootDirectory);
                GitRepository = new LibGit2Sharp.Repository(RootDirectory);
            });
    }
}