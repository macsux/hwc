using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.NukeExtensions;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using Nuke.Compilation;
using Nuke.Components.Build;
using Nuke.Components.Versioning;
using OutputType = Nuke.Compilation.OutputType;

namespace Nuke.Components.Package
{
    public interface IPackageZip : IBuildSolution
    {
        
        // List<Project> PackageZipIn { get => this.Get(); set => this.Set(value); } 
        // List<AbsolutePath> PackageZipOut { get => this.Get(); set => this.Set(value); }

        AbsolutePath PackageZipDirectory => ArtifactsDirectory / "package"  / "zip";

        List<PublishCombination> PackageZipPublishCombinations => PublishCombinations
            .Where(x => x.Project.GetOutputType() != OutputType.DynamicallyLinkedLibrary.Value)
            .ToList();
        
        Target PackageZip => _ => _
            .DependsOn(Publish)
            .Executes(() =>
            {
                PackageZipPublishCombinations.ForEach(publishProfile =>
                    {
                        var archiveName = PackageZipDirectory / GetPackageZipFileName(publishProfile);
                        var publishDir = GetPublishDirectory(publishProfile);
                        FileSystemTasks.DeleteFile(archiveName);
                        CompressionTasks.CompressZip(publishDir, archiveName);
                    });
            });
        
        
        
        string GetPackageZipFileName(PublishCombination profile)
        {
            var versionSegment = this is IVersion version ? $"-v{version.GitVersion.NuGetPackageVersion}" : "";
            return (string.Join('-', new[] {profile.Project.Name, profile.Framework, profile.RuntimeIdentifier}
                .Where(x => x != null)) + $"{versionSegment}.zip");
        }


    }
}