using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Ductus.FluentDocker.Builders;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.Build.Evaluation;
using NuGet.Packaging;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Compilation;
using Nuke.Components.Build;
using Nuke.NukeExtensions;
using Nuke.Utils;
using Project = Nuke.Common.ProjectModel.Project;

namespace Nuke
{
    public static class BuildExtensions
    {
        public static ITargetDefinition Executes<TOutput>(this ITargetDefinition targetDefinition, Func<ICollection<TOutput>> output, Func<IEnumerable<TOutput>> action)
        {
            return targetDefinition.Executes(() =>
            {
                var results = action();
                var to = output();
                to.AddRange(results);
            });
        }

        public static IList<T> SingletonList<T>(this T value) => new List<T>() {value};
        
        public static ITargetDefinition ExecutesToFolder(this ITargetDefinition targetDefinition, Func<AbsolutePath> output, Func<ICollection<AbsolutePath>> resultsList, Action<AbsolutePath> action)
        {
            return targetDefinition.Executes(() =>
            {
                var artifacts = IBuildSolution.ResultsToFolder(output(), action);
                resultsList().AddRange(artifacts);
            });
        }
        
        public static T As<T>(this INukeBuild build) where T : INukeBuild => (T) build;
        public static bool IsGitHubRepository(this LibGit2Sharp.Repository repository) 
            => repository?.Network.Remotes
                .Where(x => x.Name == "origin")
                .Select(x => x.Url.Contains("github.com"))
                .FirstOrDefault() ?? false;

        public static bool IsPublishArtifact(this Project project) =>
            project.GetProperty<bool?>(nameof(IsPublishArtifact)) ??
            project.GetOutputType().Equals("exe", StringComparison.InvariantCultureIgnoreCase);
        
        public static bool IsNugetArtifact(this Project project)
        {
            // only true if explicitly defined inside project file
            var prop = project.GetMSBuildProject().GetProperty("PackageId");
            return prop.UnevaluatedValue != "$(AssemblyName)" || prop.Xml.ContainingProject.FullPath == project.Path;
        }

        public static bool IsBuildProject(this Project project) => project.Name is "_build" or "_buildGenerator";
        public static bool RequiresWindows(this Solution solution) => solution.AllProjects
            .SelectMany(x => x.GetTargetFrameworks())
            .Any(x => Regex.IsMatch(x, "net[3-4]"));

        // public static string GetDockerImage(this Project project, bool sdk)
        // {
        //     
        // }
        public static IReadOnlyCollection<string> GetTargetFrameworksEx(this Project project)
        {
            return GetTargetFrameworksEx(project.GetMSBuildProjectEx());

        }

        public static IReadOnlyCollection<string> GetTargetFrameworksEx(this Microsoft.Build.Evaluation.Project msbuildProject)
        {
            var targetFrameworkProperty = msbuildProject.GetProperty("TargetFramework");
            if (targetFrameworkProperty != null)
                return new[] { targetFrameworkProperty.EvaluatedValue };

            var targetFrameworksProperty = msbuildProject.GetProperty("TargetFrameworks");
            if (targetFrameworksProperty != null)
                return targetFrameworksProperty.EvaluatedValue.Split(';');

            return new string[0];
        }
        public static string[] GetTargetFrameworkIdentifiers(this Project project) => 
            project
                .GetTargetFrameworksEx()
                .DefaultIfEmpty()
                .Select(tfm => project.GetMSBuildProjectEx(targetFramework: tfm).GetPropertyValue("TargetFrameworkIdentifier"))
                .ToArray();

        //
        //
        // public static IReadOnlyCollection<CompilationTarget> GetCompilationTargets(this Project project, string configuration = null)
        // {
        //     var msbuildProject = project.GetMSBuildProjectEx(configuration);
        //     var tfms = new string[0];
        //     var rids = new string[0];
        //     if (msbuildProject.TryGetValue("TargetFrameworks", out var targetFrameworks))
        //         tfms = targetFrameworks.Split(';');
        //     else if (msbuildProject.TryGetValue("TargetFramework", out var targetFramework))
        //         tfms = new[] {targetFramework};
        //     else if (msbuildProject.TryGetValue("TargetFrameworkIdentifier", out var targetFrameworkIdentifier) &&
        //              (TargetFrameworkIdentifier) targetFrameworkIdentifier == TargetFrameworkIdentifier.NetFramework)
        //     {
        //         var version = msbuildProject.GetProperty("_TargetFrameworkVersionWithoutV").EvaluatedValue;
        //         tfms = new[] {$"net{version.Replace(".","")}"};
        //     }
        //     else
        //     {
        //         throw new Exception("Can't determine target framework");
        //     }
        //     
        //     if (msbuildProject.TryGetValue("RuntimeIdentifiers", out var runtimeIdentifiers))
        //         rids = runtimeIdentifiers.Split(';');
        //     else if (msbuildProject.TryGetValue("RuntimeIdentifier", out var runtimeIdentifier))
        //         rids = new[] {runtimeIdentifier};
        //     
        //     var compilationTargets = 
        //         from tfm in tfms.DefaultIfEmpty()
        //         from rid in rids.DefaultIfEmpty()
        //         select new CompilationTarget(project.GetMSBuildProjectEx(configuration, tfm, rid));
        //
        //     return compilationTargets.ToArray();
        // }
        
        public static List<PublishCombination> GetPublishCombinations(this Solution solution) =>
            solution.AllProjects
                .SelectMany(project =>
                {
                    var msbuildProject = project.GetMSBuildProject();
                    var configurations = msbuildProject.GetPropertyValues<string>("Configurations");
                    var targetFrameworks = msbuildProject.GetPropertyValues<string>("TargetFrameworks")
                        .DefaultIfEmpty(msbuildProject.GetPropertyValue<string>("TargetFramework"));
                    var runtimeIdentifiers =  msbuildProject.GetPropertyValues<string>("RuntimeIdentifiers")
                        .DefaultIfEmpty(msbuildProject.GetPropertyValue<string>("RuntimeIdentifier", allowImported: false));

                    return from framework in targetFrameworks
                        from configuration in configurations
                        from rid in runtimeIdentifiers
                        select new PublishCombination(project, configuration, framework, rid, rid != null);
                }).ToList();

        public static FileBuilder From(this ImageBuilder builder, string from, string asLabel) => builder.From($"{from} as {asLabel}");
        public static FileBuilder From(this FileBuilder builder, string from, string asLabel) => builder.UseParent($"{from} as {asLabel}");
        public static FileBuilder Copy(this FileBuilder builder, string stageLabel, string from, string to) => builder.Copy($"--from={stageLabel} {from}", to);
        


        #region Extended project MSBuild

        public static T GetPropertyValue<T>(this Microsoft.Build.Evaluation.Project project, string property, bool allowImported = true) where T : class
        {
            if (project.TryGetValue(property, out var value))
                return (T) TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
            else
                return default;
        }
        public static T[] GetPropertyValues<T>(this Microsoft.Build.Evaluation.Project project, string property, bool allowImported = true, string delimiter = ";") where T : class
        {
            project.TryGetValue(property, out var value);
            return value?.Split(delimiter).Select(x => (T) TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(x)).ToArray() ?? new T[0];
        }

        public static bool TryGetValue(this Microsoft.Build.Evaluation.Project project, string property, out string value) =>
            project.TryGetValue(property, true, out value);
        public static bool TryGetValue(this Microsoft.Build.Evaluation.Project project, string propertyName, bool allowImported, out string value)
        {
            var property = project.GetProperty(propertyName);
            value = null;
            if (property == null || (property.IsImported && !allowImported))
                return false;
            value = property.EvaluatedValue;
            if (string.IsNullOrEmpty(value))
                value = null;
            return value != null;
        }
        
        private static Lazy<string> s_msbuildPathResolver = new Lazy<string>(() =>
        {
            var dotnet = ToolPathResolver.TryGetEnvironmentExecutable("DOTNET_EXE") ??
                         ToolPathResolver.GetPathExecutable("dotnet");
            var output = ProcessTasks.StartProcess(dotnet, "--info", logOutput: false).AssertZeroExitCode().Output;
            var basePath = (AbsolutePath) output
                .Select(x => x.Text.Trim())
                .Single(x => x.StartsWith("Base Path:"))
                .TrimStart("Base Path:").Trim();

            return (string) (basePath / "MSBuild.dll");
        });
        public static Microsoft.Build.Evaluation.Project GetMSBuildProjectEx(
            this Project project,
            string configuration = null,
            string targetFramework = null,
            string runtimeIdentifier = null)
        {
            return ParseProject(project.Path, configuration, targetFramework, runtimeIdentifier);
        }
        
        private static Microsoft.Build.Evaluation.Project ParseProject(
            string projectFile,
            string configuration = null,
            string targetFramework = null,
            string runtimeIdentifier = null)
        {
            
            Dictionary<string, string> GetProperties([CanBeNull] string configuration, [CanBeNull] string targetFramework)
            {
                var properties = new Dictionary<string, string>();
                if (configuration != null)
                    properties.Add("Configuration", configuration);
                if (targetFramework != null)
                    properties.Add("TargetFramework", targetFramework);
                if (runtimeIdentifier != null)
                    properties.Add("RuntimeIdentifier", runtimeIdentifier);
                AbsolutePath target = (AbsolutePath)ToolPathResolver.GetPackageExecutable("MSBuild.Microsoft.VisualStudio.Web.targets", "Microsoft.WebApplication.targets");
                var vsToolsPath = target.Parent.Parent;
                properties.Add("VSToolsPath", vsToolsPath);
                return properties;
            }
            var msbuildPath = Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH");

            try
            {
                if (string.IsNullOrEmpty(msbuildPath))
                    Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", s_msbuildPathResolver.Value);
                
                var projectCollection = new ProjectCollection();
                var projectRoot = Microsoft.Build.Construction.ProjectRootElement.Open(projectFile, projectCollection, preserveFormatting: true);
                var msbuildProject = Microsoft.Build.Evaluation.Project.FromProjectRootElement(projectRoot, new Microsoft.Build.Definition.ProjectOptions
                {
                    GlobalProperties = GetProperties(configuration, targetFramework),
                    ToolsVersion = projectCollection.DefaultToolsVersion,
                    ProjectCollection = projectCollection
                });

                var targetFrameworks = msbuildProject.AllEvaluatedItems
                    .Where(x => x.ItemType == "_TargetFrameworks")
                    .Select(x => x.EvaluatedInclude)
                    .OrderBy(x => x).ToList();

                if (targetFramework == null && targetFrameworks.Count > 1)
                {
                    projectCollection.UnloadProject(msbuildProject);
                    targetFramework = targetFrameworks.First();

                    Logger.Warn($"Project '{projectFile}' has multiple target frameworks ({targetFrameworks.JoinComma()}).");
                    Logger.Warn($"Evaluating using '{targetFramework}'...");

                    msbuildProject = new Microsoft.Build.Evaluation.Project(
                        projectFile,
                        GetProperties(configuration, targetFramework),
                        projectCollection.DefaultToolsVersion,
                        projectCollection);
                }

                return msbuildProject;
            }
            finally
            {
                Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildPath);
            }
        }
        #endregion
    }
}