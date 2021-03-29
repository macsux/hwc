using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nuke.Generator.Shims;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Nuke.Generator
{
    [Generator]
    public class BuilderSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var rootDirectory = TryGetRootDirectoryFrom(Path.GetDirectoryName(context.Compilation.SyntaxTrees.First().FilePath));
            var slnPath = GetSolutionFileFromConfigurationFile(rootDirectory);
            var projects = GetProjectNames(slnPath);
            var projectsClassGeneratedSource = CompilationUnit()
                .AddUsings(UsingDirective(IdentifierName("Nuke.Common.ProjectModel")))
                .AddMembers(
                    ClassDeclaration("Build")
                        .AddModifiers(Token(SyntaxKind.PartialKeyword))
                        .AddMembers(
                            ClassDeclaration("ProjectList")
                                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                                .AddMembers(projects
                                    .Select(x => x.Replace(".","").Replace("-",""))
                                    .Select(project => ParseMemberDeclaration($@"public Project {project} => this.Build.Solution.GetProject(""{project}"");"))
                                    .ToArray())))
                .NormalizeWhitespace()
                .ToFullString();
            context.AddSource("Build.Projects.gen.cs", projectsClassGeneratedSource);
        }

        private IEnumerable<string> GetProjectNames(string solutionPath)
        {
            static string GuidPattern(string text)
                => $@"\{{(?<{Regex.Escape(text)}>[0-9a-fA-F]{{8}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{12}})\}}";

            static string TextPattern(string name)
                => $@"""(?<{Regex.Escape(name)}>[^""]*)""";

            var projectRegex = new Regex(
                $@"^Project\(""{GuidPattern("typeId")}""\)\s*=\s*{TextPattern("name")},\s*{TextPattern("path")},\s*""{GuidPattern("projectId")}""$");

            var content = File.ReadAllLines(solutionPath);
            for (var i = 0; i < content.Length; i++)
            {
                var match = projectRegex.Match(content[i]);
                if (!match.Success)
                    continue;

                var path = match.Groups["path"].Value;
                if (!path.EndsWith(".csproj"))
                    continue;
                var name = match.Groups["name"].Value;
                yield return name;
            }
        }
        private string GetSolutionFileFromConfigurationFile(string rootDirectory)
        {
            var configurationFileName = ".nuke";
            var nukeFile = Path.Combine(rootDirectory, configurationFileName);
            var solutionFileRelative = File.ReadAllLines(nukeFile).ElementAtOrDefault(0);
            var solutionFile = Path.GetFullPath(Path.Combine(rootDirectory, solutionFileRelative));
            return solutionFile;
        }
        
        
        internal static string TryGetRootDirectoryFrom(string startDirectory)
        {
            return Extensions.FindParentDirectory(new DirectoryInfo(startDirectory), x => x.GetFiles(".nuke").Any()).FullName;
        }
    }
}