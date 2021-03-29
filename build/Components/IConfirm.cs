using System;
using System.Linq.Expressions;
using Nuke.Common;
using Nuke.NukeExtensions;

namespace Nuke.Components
{
    public interface IConfirm : INukeBuild
    {
        [Parameter] bool ConfirmOnPublish => this.Get();
    }
    
}