﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.Bitrise;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitLab;
using Nuke.Common.CI.Jenkins;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.CI.TravisCI;
using Nuke.Common.Tools.Git;
using Nuke.Common.ValueInjection;

namespace Nuke.Common
{
    [UsedImplicitly(ImplicitUseKindFlags.Default)]
    public class ProjectListAttribute : ValueInjectionAttributeBase
    {
        public override object GetValue(MemberInfo member, object instance)
        {
            return new Build.ProjectList((Build)instance);
        }
    }
}