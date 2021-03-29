using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using DefaultNamespace;
using Ductus.FluentDocker.Builders;
using Microsoft.Build.Locator;
using Nuke;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Compilation;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.HttpTasks;
using static Nuke.Interactive.InteractiveTasks;
using Version = LibGit2Sharp.Version;

partial class Build
{
    
    Target BuilderImage => _ => _
        .Executes(() =>
        {
            var builderImageName = $"{ToDockerImageName(Solution.Name)}-builder";
            var fileBuilder = DefineWindowsDotnetDockerImage(new Builder().DefineImage(builderImageName));
            Logger.Info(fileBuilder.ToDockerfileString());
            // var build = Projects.Hwc.GetMSBuildProjectEx().AllEvaluatedItems.ToArray();
            // var target = ToolPathResolver.GetPackageExecutable("MSBuild.Microsoft.VisualStudio.Web.targets", "Microsoft.WebApplication.targets");

            // Logger.Info(Environment.GetEnvironmentVariable("MSBUILD_EXE_PATH"));
            // return;
            // Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH",@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe");
            
            // var ids = Solution.AllProjects.SelectMany(x => x.GetTargetFrameworkIdentifiers()).ToList();
            // var ids = Projects.Blah.GetTargetFrameworkIdentifiers().ToList();
            // foreach (var item in ids)
            // {
            //     Logger.Info(item);
            // }

            return;
            
            var baseImage = Solution.RequiresWindows() ? "" : "";
            // Projects.Select(x => x.GetTargetFrameworks()).Any(x => "net[3-4]");
            
            Logger.Info(fileBuilder.ToDockerfileString());
        });

    FileBuilder DefineWindowsDotnetDockerImage(ImageBuilder builder)
    {
        var runtimesInfo = Solution.AllProjects.SelectMany(x => x.GetCompilationTargets()).ToArray();
        var coreRuntimes = runtimesInfo
            .Where(x => x.TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetCoreApp)
            .OrderBy(x => x.TargetFrameworkVersion)
            .GroupBy(x => x.TargetFrameworkVersion.ToString(2))
            .Select(x =>
            {
                var first = x.First();
                return (Version: x.Key, DockerSDKImage: first.GetDockerSDKImage(), DockerRuntimeImage: first.GetDockerRuntimeImage());
            })
            .Distinct()
            .ToArray();
        
        var coreSdk = coreRuntimes[^1]; // highest version is the SDK we gonna use
        var additionalRuntimes = coreRuntimes[0..^1];
        
        
        var frameworkRuntime = runtimesInfo.FirstOrDefault(x => x.TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetFramework);
        //1809
        // var net5sdkDockerFileUrl = HttpDownloadString("https://raw.githubusercontent.com/dotnet/dotnet-docker/a9512e00111e7ee0227bc055d97ce437f86012fe/src/sdk/5.0/windowsservercore-ltsc2019/amd64/Dockerfile");
        FileBuilder fileBuilder = null;

        FileBuilder From(string image, string label) => fileBuilder == null
            ? builder.From(image, label)
            : fileBuilder.From(image, label);

        string GetRuntimeAlias(string version) => $"coreRuntime{version}";
        
        foreach (var additionalRuntime in additionalRuntimes)
        {
            fileBuilder = From(additionalRuntime.DockerRuntimeImage, GetRuntimeAlias(additionalRuntime.Version)); 
        }

        var coreSdkAlias = "coreSdk";
        fileBuilder = From(coreSdk.DockerSDKImage, coreSdkAlias);

        if (frameworkRuntime != null)
        {
            fileBuilder = From(frameworkRuntime.GetDockerSDKImage(), "frameworkSdk");
            fileBuilder = fileBuilder.Copy(coreSdkAlias, "/Program Files/dotnet", "/Program Files/dotnet");
        }
        foreach (var additionalRuntime in additionalRuntimes)
        {
            fileBuilder = fileBuilder.Copy(GetRuntimeAlias(additionalRuntime.Version), "/Program Files/dotnet", "/Program Files/dotnet");
        }

        fileBuilder
            .Environment(new Dictionary<string, string>()
            {
                // Enable correct mode for dotnet watch (only mode supported in a container)
                {"DOTNET_USE_POLLING_FILE_WATCHER", "true"},
                // Skip extraction of XML docs - generally not useful within an image/container - helps performance
                {"NUGET_XMLDOC_MODE", "skip"},
            })
            .User("ContainerAdministrator")
            .Run(@"setx /M PATH ""%PATH%;C:\Program Files\powershell""")
            .User("ContainerUser")
            .Run("dotnet help");

        return fileBuilder;
    }
    
    string ToDockerImageName(string name) => name.Trim('_', '-').ToLower();

    
}
