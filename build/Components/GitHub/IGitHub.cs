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
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Components.Package;
using Nuke.Components.Versioning;
using Nuke.Interactive;
using Nuke.NukeExtensions;
using NukeExtensions;
using Octokit;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.CompressionTasks;
using Credentials = Octokit.Credentials;
using NotFoundException = Octokit.NotFoundException;
using GitHubRepository = Octokit.Repository;

namespace Nuke.Components.GitHub
{
    public interface IGitHub : INukeBuild, IVersion, IPackageArtifacts, INukeBuildEventsAware
    {
        [Parameter("GitHub repository url")] 
        string GitHubUrl
        {
            get => this.Get();
            set => this.Set(value);
        }

        [Parameter("Release name when creating or updating artifacts to GitHub. Defaults to 'v{NUGET_VERSION}`")] 
        string GitHubReleaseName
        {
            get => this.Get();
            set => this.Set(value);
        }

        [Parameter("GitHub personal access token with access to the repo")] 
        string GitHubToken
        {
            get => this.Get();
            set => this.Set(value);
        }

        [GitHubClient] GitHubClient GitHubClient
        {
            get => this.Get();
            set => this.Set(value);
        }

        string GitHubNugetPackageFeedUrl => $"https://nuget.pkg.github.com/{GitHubRepoOwner}/index.json";

        string GitHubRepoOwner
        {
            get => this.Get();
            set => this.Set(value);
        }
        
        string GitHubRepoName
        {
            get => this.Get();
            set => this.Set(value);
        }


        void INukeBuildEventsAware.OnBuildInitialized()
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

            if (GitHubUrl != null)
            {
                var (owner, repo) = this.GetGitHubOwnerAndRepoName();
                GitHubRepoOwner ??= owner;
                GitHubRepoName ??= repo;
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
        
        Target SetupGitHubRepo => _ => _
            .DependsOn(SetupGit, AuthenticatedGitHubClient)
            .Requires(() => GitHubUrl)
            .OnlyWhenDynamic(() => !IsRemoteOriginConfigured())
            .Executes(async () =>
            {
            
                GitHubRepository githubRepo;
                try
                {
                    githubRepo = await GitHubClient.Repository.Get(GitHubRepoOwner, GitHubRepoName);
                }
                catch (NotFoundException)
                {
                    var gitHubUserName = (await GitHubClient.User.Current()).Login;
                    if (GitHubRepoOwner == gitHubUserName)
                    {
                        githubRepo = await GitHubClient.Repository.Create(new NewRepository(GitHubRepoName));
                    }
                    else
                    {
                        githubRepo = await GitHubClient.Repository.Create(GitHubRepoOwner, new NewRepository(GitHubRepoName));
                    }
                }
            
                GitRepository.Network.Remotes.Add("origin", githubRepo.CloneUrl);
                Logger.Info($"Added Git repo at {githubRepo.CloneUrl} as 'origin'");
            });

        Target GitHubRelease => _ => _
            .Description("Creates a GitHub release (or amends existing) and uploads the artifact")
            .DependsOn(AuthenticatedGitHubClient)
            .Requires(() => IsSynchronizedWithRemote, () => GitRepository.IsGitHubRepository())
            .Executes(async () =>
            {
                Release release;
                // var (gitHubOwner, repoName) = GetGitHubOwnerAndRepoName();
                try
                {
                    release = await GitHubClient.Repository.Release.Get(GitHubRepoOwner, GitHubRepoName, GitHubReleaseName);
                }
                catch (NotFoundException)
                {
                    var newRelease = new NewRelease(GitHubReleaseName)
                    {
                        Name = GitHubReleaseName,
                        Draft = false,
                        Prerelease = string.IsNullOrEmpty(GitVersion.PrereleaseVersionNoLeadingHyphen)
                    };
                    release = await GitHubClient.Repository.Release.Create(GitHubRepoOwner, GitHubRepoName, newRelease);
                }

                foreach (var existingAsset in release.Assets)
                {
                    await GitHubClient.Repository.Release.DeleteAsset(GitHubRepoOwner, GitHubRepoName, existingAsset.Id);
                }

                Logger.Info($"GitHub Release v{GitVersion.NuGetPackageVersion}");
                foreach (var artifact in PackageArtifacts)
                {
                    var releaseAssetUpload = new ReleaseAssetUpload(artifact.GetFileName(), "application/zip", File.OpenRead(artifact), null);
                    var releaseAsset = await GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload);
                    Logger.Info($"  {releaseAsset.BrowserDownloadUrl}");
                }
            });

        bool IsSynchronizedWithRemote => !GitRepository.RetrieveStatus().IsDirty && GitRepository.Head.TrackedBranch != null &&
                                         GitRepository.ObjectDatabase.CalculateHistoryDivergence(GitRepository.Head.Tip, GitRepository.Head.TrackedBranch.Tip).AheadBy == 0 &&
                                         GitRepository.ObjectDatabase.CalculateHistoryDivergence(GitRepository.Head.Tip, GitRepository.Head.TrackedBranch.Tip).BehindBy == 0;
        
        (string owner, string repoName) GetGitHubOwnerAndRepoName()
        {
            var match = Regex.Match(GitHubUrl, "https://github.com/(?<owner>.+?)/(?<repo>.+?)(.git|/)?$");
            if (!match.Success)
                throw new InvalidOperationException("Invalid github url");
            return (match.Groups["owner"].Value, match.Groups["repo"].Value);
        }
    }
}