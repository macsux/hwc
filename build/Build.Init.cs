// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Reflection;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using DefaultNamespace;
// using HarmonyLib;
// using LibGit2Sharp;
// using Microsoft.Build.Tasks;
// using Nuke;
// using Nuke.Common;
// using Nuke.Common.CI;
// using Nuke.Common.Execution;
// using Nuke.Common.Git;
// using Nuke.Common.IO;
// using Nuke.Common.ProjectModel;
// using Nuke.Common.Tooling;
// using Nuke.Common.Tools.DotNet;
// using Nuke.Common.Tools.Git;
// using Nuke.Common.Tools.GitHub;
// using Nuke.Common.Tools.NerdbankGitVersioning;
// using Nuke.Common.Utilities.Collections;
// using NukeExtensions;
// using Octokit;
// using static Nuke.Common.EnvironmentInfo;
// using static Nuke.Common.IO.FileSystemTasks;
// using static Nuke.Common.IO.PathConstruction;
// using static Nuke.Common.Tools.DotNet.DotNetTasks;
// using static Nuke.Common.IO.CompressionTasks;
// using static Nuke.Interactive.InteractiveTasks;
// using static Nuke.Common.Tools.Git.GitTasks;
// using static Nuke.Common.Tools.NerdbankGitVersioning.NerdbankGitVersioningTasks;
// using Credentials = Octokit.Credentials;
// using NotFoundException = Octokit.NotFoundException;
// using Repository = Octokit.Repository;
// using Signature = LibGit2Sharp.Signature;
//
// public partial class Build
// {
//     [Parameter("Set to false if using init.ps1")] bool ApplyInitializerTargets = true;
//
//     Target SetupGit => _ => _
//         .Unlisted()
//         .OnlyWhenDynamic(() => !IsGitInitialized() )
//         .Executes(() =>
//         {
//             
//             LibGit2Sharp.Repository.Init(RootDirectory);
//             GitRepository = new LibGit2Sharp.Repository(RootDirectory);
//         });
//
//     Target SetupGitVersion => _ => _
//         .Unlisted()
//         .DependsOn(SetupGit)
//         .Triggers(EnsureCommit)
//         .OnlyWhenDynamic(
//             () => IsGitInitialized(),
//             () => !FileExists(RootDirectory / "version.json"))
//         .Executes(() => NerdbankGitVersioningInstall());
//
//     Target EnsureCommit => _ => _
//         .Unlisted()
//         .OnlyWhenDynamic(
//             () => LibGit2Sharp.Repository.IsValid(RootDirectory),
//             () => !GitRepository.Commits.Any())
//         .Executes(() =>
//         {
//             var signature = GitRepository.Config.BuildSignature(DateTimeOffset.UtcNow);
//             Commands.Stage(GitRepository, "*");
//             GitRepository.Commit("Initial", signature, signature);
//         });
//
//     
//     Target SetupGitHubRepo => _ => _
//         .DependsOn(SetupGit, AuthenticatedGitHubClient)
//         .Requires(() => GitHubUrl)
//         .OnlyWhenDynamic(() => !IsRemoteOriginConfigured())
//         .Executes(async () =>
//         {
//             
//             Repository githubRepo;
//             var (owner, repoName) = GetGitHubOwnerAndName();
//             try
//             {
//                 githubRepo = await GitHubClient.Repository.Get(owner, repoName);
//             }
//             catch (NotFoundException)
//             {
//                 var gitHubUserName = (await GitHubClient.User.Current()).Login;
//                 if (owner == gitHubUserName)
//                 {
//                     githubRepo = await GitHubClient.Repository.Create(new NewRepository(repoName));
//                 }
//                 else
//                 {
//                     githubRepo = await GitHubClient.Repository.Create(owner, new NewRepository(repoName));
//                 }
//             }
//             
//             GitRepository.Network.Remotes.Add("origin", githubRepo.CloneUrl);
//             Logger.Info($"Added Git repo at {githubRepo.CloneUrl} as 'origin'");
//         });
//
//    
//     
//     Target Init => _ => _ 
//         .Executes(() =>
//         {
//             var targetsToCall = new List<string>();
//             bool HasTarget(Expression<Func<Build, Target>> target) => targetsToCall.Contains(GetMemberName(target));
//             void AddTarget(Expression<Func<Build, Target>> target) => targetsToCall.Add(GetMemberName(target));
//             // MemberInfo GetMember<T>(Expression<Func<Build, T>> expression) => typeof(Build).GetMember(GetMemberName(expression), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
//             string GetMemberName<T>(Expression<Func<Build, T>> expression) => ((MemberExpression)expression.Body).Member.Name;
//             bool IsGitHubRepository() => HasTarget(x => x.SetupGitHubRepo) || GitRepository.IsGitHubRepository();
//             bool IsGitVersionSetup() => HasTarget(x => x.SetupGitVersion) || FileExists(RootDirectory / "version.json");
//             void CreateBuildPlanFile()
//             {
//                 StringBuilder sb = new();
//                 sb.AppendJoin(" ", targetsToCall);
//                 var parameters = GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//                     .Where(x => x.HasCustomAttribute<ParameterAttribute>() && x.Name is not nameof(InvokedTargets) and not nameof(SkippedTargets))
//                     .Select(x => (x.Name, Value: x.GetValue(this)))
//                     .Where(x => x.Value != null)
//                     .ToList();
//                 foreach (var (name, value) in parameters)
//                 {
//                     sb.Append(" --");
//                     sb.Append(ToSpinalCase(name));
//                     sb.Append(' ');
//                     sb.Append(value);
//                 }
//                 var varsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "nuke.tmp");
//                 File.Delete(varsFile);
//                 File.WriteAllText(varsFile, sb.ToString());
//
//
//             }
//             
//             if (!IsGitVersionSetup() && Confirm("Do you want to setup Git Versioning?"))
//             {
//                 AddTarget(x => x.SetupGitVersion);
//             }
//             
//             if (!IsGitHubRepository() && Confirm("Do you want to setup GitHub repo?"))
//             {
//                 var owner = Prompt("Who's the owner of the repo? (Your username or org name under which repo should be created)");
//                 var repoName = Prompt("What is the name of the repo?", ToSpinalCase(Solution.Name));
//                 GitHubUrl = $"https://github.com/{owner}/{repoName}.git";
//                 AddTarget(x => x.SetupGitHubRepo);
//             }
//
//             CreateBuildPlanFile();
//             
//             if (ApplyInitializerTargets && targetsToCall.Any())
//             {
//                 DotNet(Assembly.GetExecutingAssembly().Location);
//             }
//         });
//
//     static string ToSpinalCase(string input) => Regex.Replace(input, @"([A-Z])(?=[a-z]|$)", match => $"-{match.Value.ToLower()}").TrimStart('-');
//
//     (string owner, string repoName) GetGitHubOwnerAndName()
//     {
//         var match = Regex.Match(GitHubUrl, "https://github.com/(?<owner>.+?)/(?<repo>.+?)(.git|/)?$");
//         if (!match.Success)
//             throw new InvalidOperationException("Invalid github url");
//         return (match.Groups["owner"].Value, match.Groups["repo"].Value);
//     }
//
//
// }