using System;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;
using System.Net;
using System.Globalization;
using System.Linq;

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
            object attr = attributeProvider.GetCustomAttributes(typeof (T), true).FirstOrDefault();
            if (attr != null)
                return (T) attr;
            else
                return null;
        }
    }


    
    
}