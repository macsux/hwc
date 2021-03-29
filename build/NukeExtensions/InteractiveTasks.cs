
using System;
using System.IO;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitVersion;

namespace Nuke.Interactive
{


    public static class InteractiveTasks
    {
        public static bool Confirm(string prompt) => Confirm(prompt, true);

        public static bool Confirm(string prompt, bool defaultResponse)
        {
            Console.Write($"{prompt}: Y/N ");
            Console.WriteLine($"(default {(defaultResponse ? "Y" : "N")})");
            char key = '0';
            do
            {
                key = Console.ReadKey(true).KeyChar;
                if (key is '\n' or '\r')
                    key = defaultResponse ? 'y' : 'n';
            } while (key is not 'y' and not 'n');

            return key == 'y';
        }

        public static string Prompt(string prompt, string defaultResponse = null, bool allowNull = false)
        {
            Console.Write($"{prompt}: ");
            if (defaultResponse != null)
                Console.Write($"(press ENTER to use '{defaultResponse})'");
            Console.WriteLine();
            string result = null;
            do
            {
                result = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(result))
                    result = null;
                if (defaultResponse != null && result == null)
                    result = defaultResponse;
            } while (result == null && !allowNull);

            return result;
        }

        public static bool TryPrompt(string prompt, out string result)
        {
            Console.Write($"{prompt}: ");
            Console.WriteLine();
            result = Console.ReadLine();
            return !string.IsNullOrEmpty(result);
        }

        public static string GetFileName(this AbsolutePath path) => Path.GetFileName(path);
    }
}
