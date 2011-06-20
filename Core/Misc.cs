using System;
using System.Reflection;

namespace MOUSE.Core
{
    public static class AttributesExt
    {
        public static bool ContainsAttribute<T>(this ICustomAttributeProvider attributeProvider) where T : Attribute
        {
            return attributeProvider.GetCustomAttributes(typeof(T), true).Length == 1;
        }

        public static T GetAttribute<T>(this ICustomAttributeProvider attributeProvider) where T : Attribute
        {
            return (T)attributeProvider.GetCustomAttributes(typeof(T), true)[0];
        }
    }
    
}