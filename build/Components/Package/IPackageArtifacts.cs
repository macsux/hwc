using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.NukeExtensions;

namespace Nuke.Components.Package
{
    public interface IPackageArtifacts : INukeBuild, INuget, IPackageZip
    {
        List<AbsolutePath> PackageArtifacts { get => this.Get(); set => this.Set(value); }

        Target Package => _ => _
            .DependsOn<INuget>(x => x.NugetPackage)
            .DependsOn<IPackageZip>(x => x.PackageZip);
    }
}