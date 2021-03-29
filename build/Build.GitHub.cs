using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DefaultNamespace;
using LibGit2Sharp;
using Nuke;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;
using Nuke.Interactive;
using NukeExtensions;
using Octokit;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.CompressionTasks;
using Credentials = Octokit.Credentials;
using NotFoundException = Octokit.NotFoundException;

public partial class Build
{
    [GitRepositoryExt] LibGit2Sharp.Repository GitRepository;
    [GitHubClient] GitHubClient GitHubClient;
    [Parameter("GitHub repository url")] string GitHubUrl;
    [Parameter("Release name when creating or updating artifacts to GitHub. Defaults to 'v{NUGET_VERSION}`")] string GitHubReleaseName;
    [Parameter("GitHub personal access token with access to the repo")] string GitHubToken;

    [Initialize] 
    void OnBuildInitialized_GitHub()
    {
        if (GitHubUrl == null && GitRepository != null && GitRepository.IsGitHubRepository())
        {
            GitHubUrl = GitRepository.Network.Remotes
                .Where(x => x.Name == "origin")
                .Select(x => x.Url)
                .FirstOrDefault();
           
        }
        if (GitVersion != null && GitHubReleaseName == null)
        {
            GitHubReleaseName = $"v{GitVersion.Version}";
        }
    }

    Target AuthenticatedGitHubClient => _ => _
        .Requires(() => GitHubToken)
        .Executes(() =>
        {
            GitHubClient = new GitHubClient(new ProductHeaderValue("nuke-build"))
            {
                Credentials = new Credentials(GitHubToken, AuthenticationType.Bearer)
            };
        });

    Target GitHubRelease => _ => _ 
        .Description("Creates a GitHub release (or amends existing) and uploads the artifact")
        .DependsOn(Pack, AuthenticatedGitHubClient)
        .Requires(() => IsSynchronizedWithRemote, () => GitRepository.IsGitHubRepository())
        .Executes(async () =>
        {
            Release release;
            var (gitHubOwner, repoName) = GetGitHubOwnerAndName();
            try
            {
                release = await GitHubClient.Repository.Release.Get(gitHubOwner, repoName, GitHubReleaseName);
            }
            catch (NotFoundException)
            {
                var newRelease = new NewRelease(GitHubReleaseName)
                {
                    Name = GitHubReleaseName, 
                    Draft = false, 
                    Prerelease = string.IsNullOrEmpty(GitVersion.PrereleaseVersionNoLeadingHyphen)
                };
                release = await GitHubClient.Repository.Release.Create(gitHubOwner, repoName, newRelease);
            }

            foreach (var existingAsset in release.Assets)
            {
                await GitHubClient.Repository.Release.DeleteAsset(gitHubOwner, repoName, existingAsset.Id);
            }

            Logger.Info($"GitHub Release v{GitVersion.NuGetPackageVersion}");
            foreach (var artifact in GetPackArtifacts())
            {
                var releaseAssetUpload = new ReleaseAssetUpload(artifact.GetFileName(), "application/zip", File.OpenRead(artifact), null);
                var releaseAsset = await GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload);
                Logger.Info($"  {releaseAsset.BrowserDownloadUrl}");
            }
        });
    
    
    bool IsSynchronizedWithRemote => !GitRepository.RetrieveStatus().IsDirty && GitRepository.Head.TrackedBranch != null &&
                                GitRepository.ObjectDatabase.CalculateHistoryDivergence(GitRepository.Head.Tip, GitRepository.Head.TrackedBranch.Tip).AheadBy == 0 &&
                                GitRepository.ObjectDatabase.CalculateHistoryDivergence(GitRepository.Head.Tip, GitRepository.Head.TrackedBranch.Tip).BehindBy == 0;
}