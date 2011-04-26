using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MOUSE.Core;

namespace CompiledProtocol
{
    static class TTHelper
    {
        static void Main()
        {
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "Protocol").FirstOrDefault();

            //gen protocol

            foreach (var type in asm.GetTypes().Where(x => Attribute.IsDefined(x, typeof(NodeEntityContractAttribute))))
                GenerateEntity(type);

        }

        private static void GenerateEntity(Type entity)
        {
            var entityDesc = (NodeEntityContractAttribute)Attribute.GetCustomAttribute(entity, typeof (NodeEntityContractAttribute));
            //gen entity
            foreach (MethodInfo method in entity.GetMethods())
                GenerateOperation(method);
        }

        private static void GenerateOperation(MethodInfo method)
        {
            var entityDesc = (NodeEntityOperationAttribute)Attribute.GetCustomAttribute(method, typeof(NodeEntityContractAttribute));
            

        }
    }
}
