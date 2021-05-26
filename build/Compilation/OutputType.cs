using System.ComponentModel;
using Nuke.Common.Tooling;
using Nuke.Utils;

namespace Nuke.Compilation
{
    [TypeConverter(typeof(TypeConverterUnrestricted<OutputType>))]
    public class OutputType : UnrestrictedEnumeration
    {
        public static OutputType ConsoleApplication = (OutputType) "Exe";
        public static OutputType WindowsApplication = (OutputType) "WinExe";
        public static OutputType DynamicallyLinkedLibrary = (OutputType) "Library";
        public new string Value
        {
            get => base.Value;
            init => base.Value = value;
        }

        public static explicit operator OutputType(string value) => !string.IsNullOrEmpty(value) ? new() { Value = value } : null;
    }
}