using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using DefaultNamespace;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nuke;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.ValueInjection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

partial class Build
{
    
    // Target GenerateMetadata => _ => _
    //     .Unlisted()
    //     .Executes(() =>
    //     {
    //         var projectsClassGeneratedSource = CompilationUnit()
    //             .AddUsings(UsingDirective(IdentifierName(typeof(Project).Namespace!)))
    //             .AddMembers(
    //                 ClassDeclaration(nameof(Build))
    //                     .AddModifiers(Token(SyntaxKind.PartialKeyword))
    //                     .AddMembers(
    //                         ClassDeclaration(nameof(ProjectList))
    //                             .AddModifiers(Token(SyntaxKind.PartialKeyword))
    //                             .AddMembers(Solution.Projects
    //                                 .Select(project => ParseMemberDeclaration(@$"public Project {project.Name} => this.Build.Solution.GetProject(""{project.Name}"");"))
    //                                 .ToArray())))
    //             .NormalizeWhitespace()
    //             .ToFullString();
    //         File.WriteAllText(BuildProjectDirectory / "Build.Projects.gen.cs", projectsClassGeneratedSource);
    //     });


    
    [ProjectList] ProjectList Projects;
    internal partial class ProjectList : IEnumerable<Project>
    {
        readonly Build Build;
        public ProjectList(Build build)
        {
            Build = build;
        }

        public IEnumerator<Project> GetEnumerator() => Build.Solution.AllProjects.Where(x => !x.IsBuildProject()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    
}