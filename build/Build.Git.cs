using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DefaultNamespace;
using LibGit2Sharp;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;
using Nuke.Interactive;
using NukeExtensions;
using Octokit;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.CompressionTasks;
using Credentials = Octokit.Credentials;
using NotFoundException = Octokit.NotFoundException;

public partial class Build
{
    
    static bool IsGitInitialized() => LibGit2Sharp.Repository.IsValid(RootDirectory);

    bool IsCurrentBranchCommitted() => GitRepository.RetrieveStatus().IsDirty;
    bool IsRemoteOriginConfigured()
    {
        if (!IsGitInitialized())
            return false;
        using var repo = new LibGit2Sharp.Repository(RootDirectory);
        var origin = repo.Network.Remotes.FirstOrDefault(x => x.Name == "origin");
        return origin != null;
    }
}