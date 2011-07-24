using System;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;

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

    public static class Misc
    {
        private static readonly List<int> _usedIds = new List<int>();
        //full copy of .Net hash algo to stabilize it because server and client frameworks are using different algos
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe public static uint GenerateHash(string str)
        {
            int ret;
            fixed (char* chrs = str.ToCharArray())
            {
                char* chPtr = chrs;
                int num = 0x15051505;
                int num2 = num;
                int* numPtr = (int*)chPtr;
                for (int i = str.Length; i > 0; i -= 4)
                {
                    num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
                    if (i <= 2)
                    {
                        break;
                    }
                    num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
                    numPtr += 2;
                }

                ret = (num + (num2 * 0x5d588b65));
            }
            if (_usedIds.Contains((ushort)ret))
            {
                throw new Exception("Hash overlapp occurred!!!!");
            }
            _usedIds.Add(ret);
            return (uint)ret;

        }
    }
    
}