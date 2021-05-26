using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Compilation;

namespace Nuke.NukeExtensions
{
    public class ProjectInfo
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

        public OutputType OutputType { get; }
        public string AssemblyName { get; }
        public HashSet<string> RuntimeIdentifiers { get; }

        public ProjectInfo(Project project)
        {
            Project = project;
            var msbuild = Project.GetMSBuildProjectEx();
            
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