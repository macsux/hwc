using Nuke.Common.IO;

namespace Nuke.Components
{
    public abstract record Item
    {
        public abstract string Name { get; }
    }

    public record FileItem(AbsolutePath Path) : Item
    {
        public override string Name => Path;
    }
}