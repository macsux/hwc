using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Nuke.Compilation;
using Nuke.NukeExtensions;
using static Nuke.Common.Tools.DotNet.DotNetTasks;



namespace Nuke.Components.Build
{
    public interface IBuildSolution : INukeBuild
    {
        [Solution] Solution Solution => this.Get<Solution>();
        
        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        string Configuration => this.Get(NukeBuild.IsLocalBuild ? "Debug" : "Release");
        
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

        // List<PublishCombination> PublishCombinations => Solution.GetPublishCombinations()
        //     .Where(x => (RootDirectory / "src").Contains(x.Project.Directory) && x.Configuration == Configuration)
        //     .ToList();
        ICollection<PublishCombination> PublishCombinations { get; }
        
        Target Restore => _ => _ 
            .Executes(() =>
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution));
            });

        Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            });

        // AbsolutePath GetPublishDirectory(PublishCombination profile) => 
        //     PublishDirectory / profile.Project.Name / profile.Configuration / profile.Framework / profile.RuntimeIdentifier;
        AbsolutePath GetPublishDirectory(PublishCombination profile)
        {
            var segments = new[] { profile.Configuration, profile.Framework, profile.SelfContained.GetValueOrDefault() ? "sc" : "fd", profile.RuntimeIdentifier };
            return PublishDirectory / profile.Project.Name / string.Join("-", segments.Where(x => x != null));
        }

        AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";

        Target Publish => _ => _
            .Executes(() =>
            {
                FileSystemTasks.EnsureCleanDirectory(PublishDirectory);
                var publishCombinations = PublishCombinations;
                var publishCombinations2 = Solution.GetPublishCombinations();
                publishCombinations.Select(GetPublishDirectory).ForEach(x => FileSystemTasks.EnsureCleanDirectory(x));
                MSBuildTasks.MSBuild(c => c
                    .CombineWith(publishCombinations, (oo, cfg) => oo
                        .SetTargets("Publish")
                        .SetProjectFile(cfg.Project)
                        .AddProperty("TargetFramework", cfg.Framework)
                        .AddProperty("RuntimeIdentifier", cfg.RuntimeIdentifier)
                        .AddProperty("SelfContained", cfg.SelfContained)
                        .AddProperty("OutDir", TemporaryDirectory / Guid.NewGuid().ToString("N"))
                        .AddProperty("PublishDir", GetPublishDirectory(cfg))));
            });



        public static List<AbsolutePath> ResultsToFolder(AbsolutePath resultDir, Action<AbsolutePath> action)
        {
            var stagingDir = NukeBuild.TemporaryDirectory / Guid.NewGuid().ToString("N");
            action(stagingDir);
            var filesAfterTarget = stagingDir.GlobFiles("**");
            var result = new List<AbsolutePath>(filesAfterTarget.Count);
            filesAfterTarget.ForEach(file =>
            {
                var destination = resultDir / stagingDir.GetRelativePathTo(file);
                FileSystemTasks.MoveFile(file, destination, FileExistsPolicy.Overwrite);
                result.Add(destination);
            });
            FileSystemTasks.DeleteDirectory(stagingDir);
            return result;
        }
    }
}