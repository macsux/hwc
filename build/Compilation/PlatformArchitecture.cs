using System.ComponentModel;
using Nuke.Utils;

namespace Nuke.Compilation
{
    [TypeConverter(typeof(TypeConverterUnrestricted<PlatformArchitecture>))]
    public class PlatformArchitecture : UnrestrictedEnumeration
    {
        public static PlatformArchitecture AnyCPU = (PlatformArchitecture) "AnyCPU";
        public static PlatformArchitecture X86 = (PlatformArchitecture) "x86";
        public static PlatformArchitecture X64 = (PlatformArchitecture) "x64";
        public static PlatformArchitecture ARM = (PlatformArchitecture) "arm";
        public static PlatformArchitecture ARM64 = (PlatformArchitecture) "arm64";

        public static explicit operator PlatformArchitecture(string value) => !string.IsNullOrEmpty(value) ? new() { Value = value } : null;
            
    }
}