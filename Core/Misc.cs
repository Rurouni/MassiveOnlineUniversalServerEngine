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

    public static class Misc
    {
        private static readonly List<int> _usedIds = new List<int>();

        public static uint GenerateHash(string str)
        {
            return (uint)str.GetHashCode();
        }

        
    }


    public class InvalidInput : Exception
    {
        public ushort ErrorCode;

        public InvalidInput(Enum errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }

        public InvalidInput(ushort errorCode, string debugMessage)
            : base(debugMessage)
        {
            ErrorCode = errorCode;
        }

        public InvalidInput(Enum errorCode)
            : base("InvalidInput:" + errorCode)
        {
            ErrorCode = Convert.ToUInt16(errorCode);
        }
    }
    
}