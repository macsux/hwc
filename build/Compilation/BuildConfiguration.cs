using System.ComponentModel;
using Nuke.Utils;

namespace Nuke.Compilation
{
    [TypeConverter(typeof(TypeConverterUnrestricted<BuildConfiguration>))]
    public class BuildConfiguration : UnrestrictedEnumeration
    {
        public static BuildConfiguration Debug = (BuildConfiguration) "Debug";
        public static BuildConfiguration Release = (BuildConfiguration) "Release";
        public new string Value
        {
            get => base.Value;
            init => base.Value = value;
        }

        public static explicit operator BuildConfiguration(string value) => !string.IsNullOrEmpty(value) ? new() { Value = value } : null;
            
            
    }
}