using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NukeExtensions
{
    // public interface IMyComponent : IPropertyBag
    // {
    //     string Hello { get => this.Get<string>(); set => this.Set(value); }
    // }
    //
    public interface IPropertyBag
    {
        
    }
    
    public static class PropertyBag
    {
        static ConditionalWeakTable<object, Dictionary<PropertyInfo, object>> Bag = new();
        public static void Set<T>(this IPropertyBag instance, T val)
        {
            var properties = Bag.GetOrCreateValue(instance);
            var property = GetProperty();
        
            if(EqualityComparer<T>.Default.Equals(val, default(T)))
                properties.Remove(property);
            else
                properties[property] = val;
        }
        public static T Get<T>(this IPropertyBag instance)
        {

            if(Bag.TryGetValue(instance, out var properties) && properties.TryGetValue(GetProperty(), out var val))
                return (T)val;
            return default(T);
        }

        static PropertyInfo GetProperty(int depth = 2)
        {
            var method = new StackTrace().GetFrame(depth).GetMethod();
            var match = Regex.Match(method.Name, "^(get|set)_(?<name>.+)");
            if (!match.Success)
                throw new InvalidOperationException("Must be called from a property getter/setter");
            var propertyName = match.Groups["name"].Value;
            var property = method.DeclaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return property;
        }
    }
}