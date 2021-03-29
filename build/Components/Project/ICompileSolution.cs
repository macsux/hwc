using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.NukeExtensions;

namespace Nuke.Components.Project
{
    public interface ICompileSolution : INukeBuild
    {
        [Solution] Solution Solution 
        {
            get => this.Get();
            set => this.Set(value);
        }
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    }
}