using System;

namespace Nuke.Compilation
{
    public class CompilationTarget
    {
        public CompilationTarget(Microsoft.Build.Evaluation.Project project)
        {

            Configuration = project.GetPropertyValue<string>("Configuration");
            TargetFramework = project.GetPropertyValue<string>("TargetFramework") ?? throw new Exception("Project does not specify target framework");
            RuntimeIdentifier = project.GetPropertyValue<string>("RuntimeIdentifier");
            PlatformArchitecture = project.GetPropertyValue<PlatformArchitecture>("PlatformTarget") ?? project.GetPropertyValue<PlatformArchitecture>("Platform") ?? PlatformArchitecture.AnyCPU;
            TargetFrameworkIdentifier = project.GetPropertyValue<TargetFrameworkIdentifier>("TargetFrameworkIdentifier");
            TargetFrameworkVersion = Version.Parse(project.GetPropertyValue<string>("_TargetFrameworkVersionWithoutV"));
        }

        public string Configuration { get; init; }

        /// <summary>
        /// Target framework moniker (netcoreapp3.1)
        /// </summary>
        public string TargetFramework { get; init; }

        /// <summary>
        /// win-x86, linux-x64, any
        /// </summary>
        public string RuntimeIdentifier { get; init; }

        /// <summary>
        /// x86 / x64 etc
        /// </summary>
        public PlatformArchitecture PlatformArchitecture { get; init; }

        /// <summary>
        /// .NETCoreApp, .NETCoreFramework, etc
        /// </summary>
        public TargetFrameworkIdentifier TargetFrameworkIdentifier { get; init; }

        public System.Version TargetFrameworkVersion { get; }

        public string GetDockerSDKImage()
        {
            if (TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetFramework)
            {
                return TargetFrameworkVersion.Major switch
                {
                    4 => "mcr.microsoft.com/dotnet/framework/sdk:4.8",
                    3 => "mcr.microsoft.com/dotnet/framework/sdk:3.5",
                    _ => null
                };
            }

            if (TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetCoreApp)
            {
                return $"mcr.microsoft.com/dotnet/sdk:{TargetFrameworkVersion.ToString(2)}";
            }

            if (TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetStandard)
                return $"mcr.microsoft.com/dotnet/sdk:latest";
            return null;
        }

        public string GetDockerRuntimeImage()
        {
            if (TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetFramework)
            {
                return TargetFrameworkVersion.Major switch
                {
                    4 => "mcr.microsoft.com/dotnet/framework/runtime:4.8",
                    3 => "mcr.microsoft.com/dotnet/framework/runtime:3.5",
                    _ => null
                };
            }

            if (TargetFrameworkIdentifier == TargetFrameworkIdentifier.NetCoreApp)
            {
                return $"mcr.microsoft.com/dotnet/runtime:{TargetFrameworkVersion.ToString(2)}";
            }

            return null;
        }

    }
}