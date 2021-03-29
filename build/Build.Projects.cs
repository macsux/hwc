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

    partial class ProjectInfo
    {
        readonly Project Project;
        public bool Is(ProjectType projectType) => Project.Is(projectType);

        public Solution Solution => Project.Solution;

        public Guid ProjectId
        {
            get => Project.ProjectId;
            set => Project.ProjectId = value;
        }

        public string Name
        {
            get => Project.Name;
            set => Project.Name = value;
        }

        public Guid TypeId
        {
            get => Project.TypeId;
            set => Project.TypeId = value;
        }

        public SolutionFolder SolutionFolder
        {
            get => Project.SolutionFolder;
            set => Project.SolutionFolder = value;
        }

        public AbsolutePath Path => Project.Path;

        public AbsolutePath Directory => Project.Directory;

        public IDictionary<string, string> Configurations => Project.Configurations;

        public OutputKind OutputType { get; }
        public string AssemblyName { get; }
        public HashSet<string> RuntimeIdentifiers { get; }
        
        public ProjectInfo(Project project)
        {
            Project = project;
            var doc = XDocument.Load(File.OpenRead(project.Path));
            OutputType = ReadProperty(doc, "OutputType") switch
            {
                "Exe" => OutputKind.ConsoleApplication,
                "WinExe" => OutputKind.WindowsApplication,
                _ => OutputKind.DynamicallyLinkedLibrary
            };
            AssemblyName = ReadProperty(doc, "AssemblyName") ?? project.Name;
            RuntimeIdentifiers = ReadProperties(doc, "RuntimeIdentifiers").DefaultIfEmpty("any").ToHashSet();
        }

        private string ReadProperty(XDocument doc, string name)
        {
            return doc.XPathSelectElement("/Project/PropertyGroup/{name}")?.Value;
        }
        private List<string> ReadProperties(XDocument doc, string name)
        {
            return doc.XPathSelectElement("/Project/PropertyGroup/{name}")?.Value.Split(";").ToList() ?? new List<string>();

        }
    }
    
}