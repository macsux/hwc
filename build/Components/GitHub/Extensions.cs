using System.Linq;

namespace Nuke.Components.GitHub
{
    public static class Extensions
    {
        public static bool IsGitHubRepository(this LibGit2Sharp.Repository repository) 
            => repository?.Network.Remotes
                .Where(x => x.Name == "origin")
                .Select(x => x.Url.Contains("github.com"))
                .FirstOrDefault() ?? false;
    }
}