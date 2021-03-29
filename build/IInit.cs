using System.Collections.Generic;
using Nuke.Common;

public interface IInit : INukeBuild, IMyComponent
{
    
    Target InitializeGit => _ => _
        // .OnlyWhenDynamic(() => !Interactive || Confirm("Setup git?"))
        .OnlyWhenDynamic(() => !Interactive || ShouldGitInitialize)
        .Inherit<IMyComponent>(x => x.InitializeGit);

    bool ShouldGitInitialize
    {
        get => (bool)Properties[nameof(ShouldGitInitialize)];
        set => Properties[nameof(ShouldGitInitialize)] = value;
    }

    Dictionary<string, object> Properties => new Dictionary<string, object>();

    bool Interactive
    {
        get => (bool)Properties[nameof(Interactive)];
        set => Properties[nameof(Interactive)] = value;
    }


    Target Run => _ => _
        .Triggers(InitializeGit)
        .Executes(() =>
        {
            ShouldGitInitialize = true;
            Interactive = true;
        });
}
