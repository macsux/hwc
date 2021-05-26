using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Utils
{
#pragma warning disable 660,661
        public abstract class UnrestrictedEnumeration : Enumeration
#pragma warning restore 660,661
        {
            public static bool operator ==(UnrestrictedEnumeration a, UnrestrictedEnumeration b) => EqualityComparer<UnrestrictedEnumeration>.Default.Equals(a, b);
            public static bool operator !=(UnrestrictedEnumeration a, UnrestrictedEnumeration b) => !EqualityComparer<UnrestrictedEnumeration>.Default.Equals(a, b);
            public static implicit operator string(UnrestrictedEnumeration value) => value?.Value;


            public class TypeConverterUnrestricted<T> : TypeConverter where T : UnrestrictedEnumeration
            {
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                {
                    return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
                }

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                {
                    if (value == null)
                        return null;
                    if (value is string stringValue)
                    {
                        var matchingFields = typeof(T).GetFields(ReflectionService.Static)
                            .Where(x => x.FieldType.IsAssignableTo(typeof(UnrestrictedEnumeration)))
                            .Select(x => ((UnrestrictedEnumeration)x.GetValue()))
                            .Where(x => x.Value.EqualsOrdinalIgnoreCase(stringValue))
                            .ToList();
                        ControlFlow.Assert(matchingFields.Count <= 1, "matchingFields.Count > 1");
                        var result = matchingFields.FirstOrDefault();
                        if (result != null)
                            return result;
                        result = Activator.CreateInstance<T>();
                        result.Value = stringValue;
                        return result;
                    }

                    return base.ConvertFrom(context, culture, value);
                }
            }
        }
}