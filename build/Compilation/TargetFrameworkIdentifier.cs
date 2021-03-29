using System.ComponentModel;
using Nuke.Utils;

namespace Nuke.Compilation
{
    [TypeConverter(typeof(TypeConverterUnrestricted<TargetFrameworkIdentifier>))]
    public class TargetFrameworkIdentifier : UnrestrictedEnumeration
    {
        public static TargetFrameworkIdentifier NetCoreApp = (TargetFrameworkIdentifier) ".NETCoreApp";
        public static TargetFrameworkIdentifier NetFramework = (TargetFrameworkIdentifier) ".NETFramework";
        public static TargetFrameworkIdentifier NetStandard = (TargetFrameworkIdentifier) ".NETStandard";
        public static explicit operator TargetFrameworkIdentifier(string value) => !string.IsNullOrEmpty(value) ? new() { Value = value } : null;

    }
}