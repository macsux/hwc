using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultNamespace;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Nuke;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Nuke.Compilation;
using Nuke.Components;
using Nuke.Components.Build;
using Nuke.Components.Git;
using Nuke.Components.Package;
using Nuke.Components.Versioning;
using Nuke.NukeExtensions;
using NukeExtensions;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.CompressionTasks;
using Project = Nuke.Common.ProjectModel.Project;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions("mybuild", GitHubActionsImage.WindowsLatest, InvokedTargets = new[]{nameof(CI)})] 
partial class Build : NukeBuild, INuget, IGit, IBuildSolution, IPackageArtifacts
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main ()
    {
        // MSBuildLocator.RegisterDefaults();
        return Execute<Build>();
    }
    [NerdbankGitVersioning] NerdbankGitVersioning GitVersion;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";
    [Parameter] bool IncludeVersionInArtifactNames;
    // [Parameter] bool ConfirmOnPublish = true;


    [Solution] internal readonly Solution Solution;
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    AbsolutePath PackDirectory => ArtifactsDirectory / "pack";

    public ICollection<PublishCombination> PublishCombinations => new[]
        {
            
            new PublishCombination(Projects.Hwc, Configuration, SelfContained: false).Expand(),
            new PublishCombination(Projects.Logging, Configuration).Expand()
        }
        .SelectMany(x => x)
        .ToList();

    // Target Clean => _ => _
    //     .Before(Restore)
    //     .Executes(() =>
    //     {
    //         SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
    //         TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
    //         EnsureCleanDirectory(ArtifactsDirectory);
    //     });

    Target CI => _ => _
        .Executes(() =>
        {
            Logger.Info("Hello");
        });
    
    // Target Publish => _ => _
    //     .After(Test)
    //     .Description("Publishes to artifacts directory")
    //     // .Requires(() => GitVersion)
    //     .Executes(() =>
    //     {
    //         var publishRoot = ArtifactsDirectory / "publish";
    //         EnsureCleanDirectory(TemporaryDirectory);
    //         EnsureCleanDirectory(publishRoot);
    //         MSBuildTasks.MSBuild(c => c
    //             .CombineWith(GetPublishCombinations(), (oo, cfg) => oo
    //                 .SetTargets("Publish")
    //                 .SetProjectFile(cfg.Project.Path)
    //                 .SetProperties(GetMsBuildProperties(cfg.Project, cfg.Framework, cfg.Rid))));
    //     });

    // Target Pack => _ => _
    //     .DependsOn(Publish)
    //     
    //     .Executes(() =>
    //     {
    //         GetPublishCombinations()
    //             .ForEach(x =>
    //             {
    //                 var archiveName = GetPackArtifact(x.Project, x.Framework, x.Rid);
    //                 var publishDir = GetProjectPublishDirectory(x.Project, x.Framework, x.Rid);
    //                 DeleteFile(archiveName);
    //                 CompressZip(publishDir, archiveName);
    //             });
    //         DotNetPack(c => c
    //             .SetVersion(this.As<IVersion>().GitVersion.Version)  
    //             .CombineWith(Projects.Where(x => x.IsNugetArtifact()), (o, project) => o
    //                 .SetProject(project.Path)
    //                 .SetOutputDirectory(PackDirectory)));
    //     });
    
    
    // Target Package => _ => _
    //     .Triggers<INuget>(x => x.NugetPackage)
    //     .Executes(() =>
    //     {
    //         GetPublishCombinations()
    //             .ForEach(x =>
    //             {
    //                 var archiveName = GetPackArtifact(x.Project, x.Framework, x.Rid);
    //                 var publishDir = GetProjectPublishDirectory(x.Project, x.Framework, x.Rid);
    //                 DeleteFile(archiveName);
    //                 CompressZip(publishDir, archiveName);
    //             });
    //     });

    // Target Test => _ => _
    //     .Executes(() =>
    //     {
    //         GetPackArtifacts().ForEach(x => Logger.Info(x));
    //     });
    //
    // public Target Restore => _ => _ 
    //     .Executes(() =>
    //     {
    //         DotNetRestore(s => s
    //             .SetProjectFile(Solution));
    //     });
    //
    // public Target Compile => _ => _
    //     .DependsOn(Restore)
    //     .Executes(() =>
    //     {
    //         DotNetBuild(s => s
    //             .SetProjectFile(Solution)
    //             .SetConfiguration(Configuration)
    //             .EnableNoRestore());
    //     });
    //
    // IEnumerable<AbsolutePath> GetPackArtifacts() =>
    //     GetPublishCombinations().Select(x => GetPackArtifact(x.Project, x.Framework, x.Rid))
    //         .Union(Projects.Where(x => x.IsNugetArtifact()).Select(x => GetPackArtifact(x, null, null)));
    //     // PackDirectory.GlobFiles($"*{GitVersion.NuGetPackageVersion}.zip", $"*{GitVersion.NuGetPackageVersion}.nupkg");
    // Dictionary<string, object> GetMsBuildProperties(Project project, string framework, string rid) =>
    //     new()
    //     {
    //         {"TargetFramework", framework},
    //         {"RuntimeIdentifier", rid ?? ""},
    //         {"PublishDir", GetProjectPublishDirectory(project, framework, rid)}
    //     };

    // AbsolutePath GetPackArtifact(Project project, string framework, string rid)
    // {
    //     var versionSegment = IncludeVersionInArtifactNames ? $"-v{GitVersion.NuGetPackageVersion}" : "";
    //     return project.IsNugetArtifact()
    //         ? PackDirectory / $"{project.GetMSBuildProject().GetPropertyValue("PackageId")}.{GitVersion.NuGetPackageVersion}.nupkg"
    //         : PackDirectory / (string.Join('-', new[] {project.Name, framework, rid}.Where(x => x != null)) + $"{versionSegment}.zip");
    // }
    //
    // AbsolutePath GetProjectPublishDirectory(Project project, string framework, string rid) => PublishDirectory / project.Name / framework / (rid ?? "fd");
    // IEnumerable<(Project Project, string Framework, string Rid)> GetPublishCombinations() =>
    //     Projects
    //         .Where(x => x.IsPublishArtifact())
    //         .SelectMany(project =>
    //         {
    //             var msbuildProject = project.GetMSBuildProject();
    //             var targetFrameworks = (msbuildProject.GetProperty("TargetFrameworks")?.EvaluatedValue
    //                                     ?? msbuildProject.GetProperty("TargetFramework").EvaluatedValue).Split(";");
    //             var runtimeIdentifiers = (msbuildProject.GetProperty("RuntimeIdentifiers")?.EvaluatedValue
    //                                       ?? msbuildProject.GetProperty("RuntimeIdentifier")?.EvaluatedValue)?.Split(";")
    //                                      ?? new string[0];
    //
    //             return from framework in targetFrameworks
    //                 from rid in runtimeIdentifiers.DefaultIfEmpty()
    //                 select (project, framework, rid);
    //         });

    
    // protected override void OnBuildInitialized()
    // {
    //     base.OnBuildInitialized();
    //     var type = typeof(INukeBuildEventsAware);
    //     var targetMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(x => $"{x.DeclaringType.FullName.Replace("+",".")}.{x.Name}").ToHashSet();
    //     typeof(INukeBuildEventsAware)
    //         .GetInterfaces()
    //         .Where(x => 
    //             x != type && 
    //             x.IsAssignableTo(type))
    //         .SelectMany(x => x.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    //         .Where(x => targetMethods.Contains(x.Name) && !x.IsAbstract)
    //         .ForEach(x => x.Invoke(this, null));
    //     
    //     foreach (var initializeContributor in GetType()
    //         .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
    //         .Where(x => x.HasCustomAttribute<InitializeAttribute>()))
    //     {
    //         initializeContributor.Invoke(this, null);
    //     }
    // }
}
