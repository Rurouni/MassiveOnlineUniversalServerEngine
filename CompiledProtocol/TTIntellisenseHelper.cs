using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MOUSE.Core;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.CodeDom;
using Protocol.Generated;

namespace CompiledProtocol
{
    static class TTIntellisenseHelper
    {
        static void Main()
        {
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.Contains("Protocol")).FirstOrDefault();

            //gen protocol

            foreach (var type in asm.GetTypes().Where(x => x.ContainsAttribute<NodeEntityContractAttribute>()))
                GenerateAsyncProxy(type);

            GenerateDomainDesc();

        }

        
        public static void GenerateDomainDesc()
        {
            throw new NotImplementedException();
        }

        static void GenerateAsyncProxy(Type contractType)
        {
            var attr = contractType.GetAttribute<NodeEntityContractAttribute>();
            
            foreach (MethodInfo method in contractType.GetMethods())
            {
                var opAttr = method.GetAttribute<NodeEntityOperationAttribute>();
                string requestMsgName = contractType.Name+method.Name+"Request";
                string replyMsgName = contractType.Name+method.Name+"Reply";
                WriteMessage(Misc.GenerateHash(requestMsgName), requestMsgName, method.GetParameters().Select(x=>Tuple.Create(x.ParameterType, x.Name)), opAttr.Priority, opAttr.Reliability);
                if(method.ReturnType != typeof(void))
                {
                    if(method.ReturnType == typeof(Task))
                        WriteMessage(Misc.GenerateHash(replyMsgName), replyMsgName, new List<Tuple<Type, string>>(), opAttr.Priority, opAttr.Reliability);
                    else
                        WriteMessage(Misc.GenerateHash(replyMsgName), replyMsgName, new List<Tuple<Type, string>>{Tuple.Create(method.ReturnType, "RetVal")}, opAttr.Priority, opAttr.Reliability);
                }

                var funcDef = new StringBuilder();
                funcDef.AppendFormat("Task<{0}>", method.ReturnType.GetGenericArguments()[0].FullName);
                funcDef.Append(" " + contractType.Name + "." + method.Name + "(");
                var paramArr = method.GetParameters();
                for (int i = 0; i < paramArr.Length; i++)
                {
                    if(i!=0)
                        funcDef.Append(", ");
                    funcDef.Append(paramArr[i].ParameterType.FullName + " " + paramArr[i].Name);
                    
                }
                
            }
        }

        private static void WriteMessage(uint p, string requestMsgName, IEnumerable<Tuple<Type, string>> iEnumerable, MessagePriority messagePriority, MessageReliability messageReliability)
        {
            throw new NotImplementedException();
        }
    

        
    }
}
