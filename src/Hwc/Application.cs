using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NMica.Hwc
{
    public class Application
    {
        public string Name { get; set; } = "Default Web Site";
        public int Port { get; set; } = 8080;
        public string ApplicationRoot { get; set; } = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "inetpub","wwwroot");

        public static Application Parse(string[] args)
        {
            // yeah, this is very primitive and probably has edge case problems, but want to keep it slim without dependency on a arg parser library
            var application = new Application();
            var argsList = args.ToList();
            if(argsList.Contains("--help"))
                DisplayHelp();
            if (argsList.Contains("--version"))
            {
                Console.WriteLine(typeof(Application).Assembly.GetName().Version);
                Environment.Exit(0);
            }
            if (TryGetOption(argsList, "--port", out var portStr))
            {
                if(!int.TryParse(portStr, out var port))
                    DisplayHelp();
                application.Port = port;
            }

            if (TryGetOption(argsList, "--appRootDir", out var appRootDir))
            {
                if (!Path.IsPathRooted(appRootDir))
                {
                    appRootDir = Path.Combine(Directory.GetCurrentDirectory(), appRootDir);
                }

                if (!Directory.Exists(appRootDir))
                    throw new ValidationException($"{appRootDir} is not a valid application root directory");
                application.ApplicationRoot = appRootDir;
            }

            return application;
        }

        private static bool TryGetOption(List<string> args, string option, out string value)
        {
            if (args.Count % 2 != 0)
            {
                DisplayHelp();
            }
            value = null;
            var indexOf = args.IndexOf(option);
            if (indexOf >= 0)
            {
                value = args[indexOf + 1];
                return true;
                
            }

            var envVarName = option.TrimStart('-').Replace("-", "_");
            value = Environment.GetEnvironmentVariable(envVarName);
            return value != null;
        }
        
        // private static List<string> GetOptions(List<string> args, string option)
        // {
        //     if (args.Count % 2 != 0)
        //     {
        //         DisplayHelp();
        //     }
        //
        //     return args.SelectMany((arg, index) =>
        //     {
        //         if (arg == option)
        //             return new[] {args[index + 1]};
        //         return Enumerable.Empty<string>();
        //     }).ToList();
        // }

        private static void DisplayHelp()
        {
            Console.WriteLine("Usage: hwc [--appRootDir DIR] [--port PORT]");
            Console.WriteLine("--appRootDir - application directory. Default '.'");
            Console.WriteLine("--port - the port to listen on. Default 8080");
            Console.WriteLine("--version - display version");
            Console.WriteLine("--help - display this message");
            Environment.Exit(1);
        }
    }
}