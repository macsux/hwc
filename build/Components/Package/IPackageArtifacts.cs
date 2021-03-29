using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.NukeExtensions;

namespace Nuke.Components.Package
{
    public interface IPackageArtifacts : INukeBuild
    {
        List<AbsolutePath> PackageArtifacts { get => this.Get(); set => this.Set(value); }
    }
}