using System.Collections.Generic;
using System.Linq;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;
using HarmonyLib;

namespace DefaultNamespace
{
    public static class FluentDockerExtensions
    {
        public static FileBuilder Environment(this FileBuilder builder, Dictionary<string, string> values)
        {
            GetFileBuilderConfig(builder).Commands.Add(new EnvCommand(values));
            return builder;
        }
        public static FileBuilder User(this FileBuilder builder, string user)
        {
            GetFileBuilderConfig(builder).Commands.Add(new UserCommand(user));
            return builder;
        }

        private static FileBuilderConfig GetFileBuilderConfig(FileBuilder builder)
        {
            return Traverse.Create(builder).Field<FileBuilderConfig>("_config").Value;
        }

        class EnvCommand : ICommand
        {
            public Dictionary<string, string> Values { get; }

            public EnvCommand(Dictionary<string, string> values)
            {
                Values = values ?? new();
            }

            public override string ToString()
            {
                return $"ENV {string.Join(" \\\n    ", Values.Select(x => $"{x.Key}={x.Value}"))}";
            }
        }
        sealed class UserCommand : ICommand
        {
            public UserCommand(string user)
            {
                User = user;
            }

            public string User { get; }

            public override string ToString()
            {
                return $"USER {User}";
            }
        }
    }
}