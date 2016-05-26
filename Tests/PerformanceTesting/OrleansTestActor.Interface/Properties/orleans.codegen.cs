#if !EXCLUDE_CODEGEN
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 693
#pragma warning disable 1591
#pragma warning disable 1998
[assembly: global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0")]
[assembly: global::Orleans.CodeGeneration.OrleansCodeGenerationTargetAttribute("MOUSE.Core.Portable, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), global::Orleans.CodeGeneration.OrleansCodeGenerationTargetAttribute("OrleansTestActor.Interface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
namespace MOUSE.Core
{
    using global::Orleans.Async;
    using global::Orleans;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.EmptyMessage)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_EmptyMessageSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField0 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField0 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.EmptyMessage input = ((global::MOUSE.Core.EmptyMessage)original);
            global::MOUSE.Core.EmptyMessage result = new global::MOUSE.Core.EmptyMessage();
            setField0(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.EmptyMessage input = (global::MOUSE.Core.EmptyMessage)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.EmptyMessage result = new global::MOUSE.Core.EmptyMessage();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField0(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::MOUSE.Core.EmptyMessage)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.EmptyMessage), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_EmptyMessageSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.OperationResult)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_OperationResultSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField1 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField1 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field1);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.OperationResult input = ((global::MOUSE.Core.OperationResult)original);
            global::MOUSE.Core.OperationResult result = new global::MOUSE.Core.OperationResult();
            result.@Error = (global::MOUSE.Core.ErrorMessage)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(input.@Error);
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField1(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.OperationResult input = (global::MOUSE.Core.OperationResult)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@Error, stream, typeof (global::MOUSE.Core.ErrorMessage));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.OperationResult result = new global::MOUSE.Core.OperationResult();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@Error = (global::MOUSE.Core.ErrorMessage)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.ErrorMessage), stream);
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::MOUSE.Core.OperationResult)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.OperationResult), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_OperationResultSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.ErrorMessage)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_ErrorMessageSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field2 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField2 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field2);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField2 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field2);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.ErrorMessage input = ((global::MOUSE.Core.ErrorMessage)original);
            global::MOUSE.Core.ErrorMessage result = new global::MOUSE.Core.ErrorMessage();
            result.@ErrorCode = input.@ErrorCode;
            result.@ErrorString = input.@ErrorString;
            setField2(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField2(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.ErrorMessage input = (global::MOUSE.Core.ErrorMessage)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@ErrorCode, stream, typeof (global::System.UInt16));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@ErrorString, stream, typeof (global::System.String));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField2(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.ErrorMessage result = new global::MOUSE.Core.ErrorMessage();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@ErrorCode = (global::System.UInt16)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.UInt16), stream);
            result.@ErrorString = (global::System.String)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.String), stream);
            setField2(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::MOUSE.Core.ErrorMessage)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.ErrorMessage), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_ErrorMessageSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.CallbackChannelRef)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_CallbackChannelRefSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field2 = typeof (global::MOUSE.Core.CallbackChannelRef).@GetField("CallbackChannelId", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Guid> getField2 = (global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetGetter(field2);
        private static readonly global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Guid> setField2 = (global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field2);
        private static readonly global::System.Reflection.FieldInfo field3 = typeof (global::MOUSE.Core.CallbackChannelRef).@GetField("_address", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Net.IPEndPoint> getField3 = (global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Net.IPEndPoint>)global::Orleans.Serialization.SerializationManager.@GetGetter(field3);
        private static readonly global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Net.IPEndPoint> setField3 = (global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Net.IPEndPoint>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field3);
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.CallbackChannelRef).@GetField("_ip", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Byte[]> getField0 = (global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Byte[]>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Byte[]> setField0 = (global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Byte[]>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.CallbackChannelRef).@GetField("_port", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Int32> getField1 = (global::System.Func<global::MOUSE.Core.CallbackChannelRef, global::System.Int32>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Int32> setField1 = (global::System.Action<global::MOUSE.Core.CallbackChannelRef, global::System.Int32>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field1);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.CallbackChannelRef input = ((global::MOUSE.Core.CallbackChannelRef)original);
            global::MOUSE.Core.CallbackChannelRef result = new global::MOUSE.Core.CallbackChannelRef();
            setField2(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField2(input)));
            setField3(result, getField3(input));
            setField0(result, (global::System.Byte[])global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            setField1(result, getField1(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.CallbackChannelRef input = (global::MOUSE.Core.CallbackChannelRef)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField2(input), stream, typeof (global::System.Guid));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField3(input), stream, typeof (global::System.Net.IPEndPoint));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Byte[]));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Int32));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.CallbackChannelRef result = new global::MOUSE.Core.CallbackChannelRef();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField2(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream));
            setField3(result, (global::System.Net.IPEndPoint)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Net.IPEndPoint), stream));
            setField0(result, (global::System.Byte[])global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Byte[]), stream));
            setField1(result, (global::System.Int32)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Int32), stream));
            return (global::MOUSE.Core.CallbackChannelRef)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.CallbackChannelRef), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_CallbackChannelRefSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.ActorDisconnected)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_ActorDisconnectedSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField1 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField1 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field1);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.ActorDisconnected input = ((global::MOUSE.Core.ActorDisconnected)original);
            global::MOUSE.Core.ActorDisconnected result = new global::MOUSE.Core.ActorDisconnected();
            result.@Actor = input.@Actor;
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField1(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.ActorDisconnected input = (global::MOUSE.Core.ActorDisconnected)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@Actor, stream, typeof (global::MOUSE.Core.Actors.ActorRef));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.ActorDisconnected result = new global::MOUSE.Core.ActorDisconnected();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@Actor = (global::MOUSE.Core.Actors.ActorRef)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.Actors.ActorRef), stream);
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::MOUSE.Core.ActorDisconnected)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.ActorDisconnected), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_ActorDisconnectedSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.Actors.ActorRef)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_Actors_ActorRefSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field2 = typeof (global::MOUSE.Core.Actors.ActorRef).@GetField("Key", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::MOUSE.Core.Actors.ActorKey> getField2 = (global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::MOUSE.Core.Actors.ActorKey>)global::Orleans.Serialization.SerializationManager.@GetGetter(field2);
        private static readonly global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::MOUSE.Core.Actors.ActorKey> setField2 = (global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::MOUSE.Core.Actors.ActorKey>)global::Orleans.Serialization.SerializationManager.@GetValueSetter(field2);
        private static readonly global::System.Reflection.FieldInfo field3 = typeof (global::MOUSE.Core.Actors.ActorRef).@GetField("_location", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Net.IPEndPoint> getField3 = (global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Net.IPEndPoint>)global::Orleans.Serialization.SerializationManager.@GetGetter(field3);
        private static readonly global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Net.IPEndPoint> setField3 = (global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Net.IPEndPoint>)global::Orleans.Serialization.SerializationManager.@GetValueSetter(field3);
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.Actors.ActorRef).@GetField("_locationIp", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Byte[]> getField0 = (global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Byte[]>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Byte[]> setField0 = (global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Byte[]>)global::Orleans.Serialization.SerializationManager.@GetValueSetter(field0);
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.Actors.ActorRef).@GetField("_locationPort", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Int32> getField1 = (global::System.Func<global::MOUSE.Core.Actors.ActorRef, global::System.Int32>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Int32> setField1 = (global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorRef, global::System.Int32>)global::Orleans.Serialization.SerializationManager.@GetValueSetter(field1);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.Actors.ActorRef input = ((global::MOUSE.Core.Actors.ActorRef)original);
            global::MOUSE.Core.Actors.ActorRef result = default (global::MOUSE.Core.Actors.ActorRef);
            setField2(ref result, getField2(input));
            setField3(ref result, getField3(input));
            setField0(ref result, (global::System.Byte[])global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            setField1(ref result, getField1(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.Actors.ActorRef input = (global::MOUSE.Core.Actors.ActorRef)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField2(input), stream, typeof (global::MOUSE.Core.Actors.ActorKey));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField3(input), stream, typeof (global::System.Net.IPEndPoint));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Byte[]));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Int32));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.Actors.ActorRef result = default (global::MOUSE.Core.Actors.ActorRef);
            setField2(ref result, (global::MOUSE.Core.Actors.ActorKey)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.Actors.ActorKey), stream));
            setField3(ref result, (global::System.Net.IPEndPoint)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Net.IPEndPoint), stream));
            setField0(ref result, (global::System.Byte[])global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Byte[]), stream));
            setField1(ref result, (global::System.Int32)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Int32), stream));
            return (global::MOUSE.Core.Actors.ActorRef)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.Actors.ActorRef), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_Actors_ActorRefSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.Actors.ActorKey)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_Actors_ActorKeySerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.Actors.ActorKey).@GetField("Id", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Actors.ActorKey, global::System.String> getField0 = (global::System.Func<global::MOUSE.Core.Actors.ActorKey, global::System.String>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorKey, global::System.String> setField0 = (global::Orleans.Serialization.SerializationManager.ValueTypeSetter<global::MOUSE.Core.Actors.ActorKey, global::System.String>)global::Orleans.Serialization.SerializationManager.@GetValueSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.Actors.ActorKey input = ((global::MOUSE.Core.Actors.ActorKey)original);
            global::MOUSE.Core.Actors.ActorKey result = default (global::MOUSE.Core.Actors.ActorKey);
            setField0(ref result, getField0(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.Actors.ActorKey input = (global::MOUSE.Core.Actors.ActorKey)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.String));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.Actors.ActorKey result = default (global::MOUSE.Core.Actors.ActorKey);
            setField0(ref result, (global::System.String)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.String), stream));
            return (global::MOUSE.Core.Actors.ActorKey)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.Actors.ActorKey), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_Actors_ActorKeySerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.CallbackChannelDisconnected)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_CallbackChannelDisconnectedSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField1 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField1 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field1);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.CallbackChannelDisconnected input = ((global::MOUSE.Core.CallbackChannelDisconnected)original);
            global::MOUSE.Core.CallbackChannelDisconnected result = new global::MOUSE.Core.CallbackChannelDisconnected();
            result.@CallbackChannelId = (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(input.@CallbackChannelId);
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField1(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.CallbackChannelDisconnected input = (global::MOUSE.Core.CallbackChannelDisconnected)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@CallbackChannelId, stream, typeof (global::System.Guid));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.CallbackChannelDisconnected result = new global::MOUSE.Core.CallbackChannelDisconnected();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@CallbackChannelId = (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream);
            setField1(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::MOUSE.Core.CallbackChannelDisconnected)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.CallbackChannelDisconnected), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_CallbackChannelDisconnectedSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.OperationHeader)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_OperationHeaderSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field1 = typeof (global::MOUSE.Core.OperationHeader).@GetField("ActivityId", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.OperationHeader, global::System.Guid> getField1 = (global::System.Func<global::MOUSE.Core.OperationHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetGetter(field1);
        private static readonly global::System.Action<global::MOUSE.Core.OperationHeader, global::System.Guid> setField1 = (global::System.Action<global::MOUSE.Core.OperationHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field1);
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.OperationHeader).@GetField("RequestId", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.OperationHeader, global::System.Guid> getField0 = (global::System.Func<global::MOUSE.Core.OperationHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.OperationHeader, global::System.Guid> setField0 = (global::System.Action<global::MOUSE.Core.OperationHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        private static readonly global::System.Reflection.FieldInfo field2 = typeof (global::MOUSE.Core.OperationHeader).@GetField("Type", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.OperationHeader, global::MOUSE.Core.OperationType> getField2 = (global::System.Func<global::MOUSE.Core.OperationHeader, global::MOUSE.Core.OperationType>)global::Orleans.Serialization.SerializationManager.@GetGetter(field2);
        private static readonly global::System.Action<global::MOUSE.Core.OperationHeader, global::MOUSE.Core.OperationType> setField2 = (global::System.Action<global::MOUSE.Core.OperationHeader, global::MOUSE.Core.OperationType>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field2);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.OperationHeader input = ((global::MOUSE.Core.OperationHeader)original);
            global::MOUSE.Core.OperationHeader result = new global::MOUSE.Core.OperationHeader();
            setField1(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField1(input)));
            setField0(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            setField2(result, getField2(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.OperationHeader input = (global::MOUSE.Core.OperationHeader)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField1(input), stream, typeof (global::System.Guid));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Guid));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField2(input), stream, typeof (global::MOUSE.Core.OperationType));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.OperationHeader result = new global::MOUSE.Core.OperationHeader();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField1(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream));
            setField0(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream));
            setField2(result, (global::MOUSE.Core.OperationType)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.OperationType), stream));
            return (global::MOUSE.Core.OperationHeader)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.OperationHeader), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_OperationHeaderSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.CallbackHeader)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_CallbackHeaderSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.CallbackHeader).@GetField("CallbackChannelId", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.CallbackHeader, global::System.Guid> getField0 = (global::System.Func<global::MOUSE.Core.CallbackHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.CallbackHeader, global::System.Guid> setField0 = (global::System.Action<global::MOUSE.Core.CallbackHeader, global::System.Guid>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.CallbackHeader input = ((global::MOUSE.Core.CallbackHeader)original);
            global::MOUSE.Core.CallbackHeader result = new global::MOUSE.Core.CallbackHeader();
            setField0(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.CallbackHeader input = (global::MOUSE.Core.CallbackHeader)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Guid));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.CallbackHeader result = new global::MOUSE.Core.CallbackHeader();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField0(result, (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream));
            return (global::MOUSE.Core.CallbackHeader)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.CallbackHeader), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_CallbackHeaderSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.ActorTargetHeader)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_ActorTargetHeaderSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.ActorTargetHeader).@GetField("ActorKey", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.ActorTargetHeader, global::MOUSE.Core.Actors.ActorKey> getField0 = (global::System.Func<global::MOUSE.Core.ActorTargetHeader, global::MOUSE.Core.Actors.ActorKey>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.ActorTargetHeader, global::MOUSE.Core.Actors.ActorKey> setField0 = (global::System.Action<global::MOUSE.Core.ActorTargetHeader, global::MOUSE.Core.Actors.ActorKey>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.ActorTargetHeader input = ((global::MOUSE.Core.ActorTargetHeader)original);
            global::MOUSE.Core.ActorTargetHeader result = new global::MOUSE.Core.ActorTargetHeader();
            setField0(result, getField0(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.ActorTargetHeader input = (global::MOUSE.Core.ActorTargetHeader)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::MOUSE.Core.Actors.ActorKey));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.ActorTargetHeader result = new global::MOUSE.Core.ActorTargetHeader();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField0(result, (global::MOUSE.Core.Actors.ActorKey)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.Actors.ActorKey), stream));
            return (global::MOUSE.Core.ActorTargetHeader)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.ActorTargetHeader), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_ActorTargetHeaderSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::MOUSE.Core.ActorDirectReplyHeader)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenMOUSE_Core_ActorDirectReplyHeaderSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.ActorDirectReplyHeader).@GetField("ActorRef", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.ActorDirectReplyHeader, global::MOUSE.Core.Actors.ActorRef> getField0 = (global::System.Func<global::MOUSE.Core.ActorDirectReplyHeader, global::MOUSE.Core.Actors.ActorRef>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.ActorDirectReplyHeader, global::MOUSE.Core.Actors.ActorRef> setField0 = (global::System.Action<global::MOUSE.Core.ActorDirectReplyHeader, global::MOUSE.Core.Actors.ActorRef>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::MOUSE.Core.ActorDirectReplyHeader input = ((global::MOUSE.Core.ActorDirectReplyHeader)original);
            global::MOUSE.Core.ActorDirectReplyHeader result = new global::MOUSE.Core.ActorDirectReplyHeader();
            setField0(result, getField0(input));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::MOUSE.Core.ActorDirectReplyHeader input = (global::MOUSE.Core.ActorDirectReplyHeader)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::MOUSE.Core.Actors.ActorRef));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::MOUSE.Core.ActorDirectReplyHeader result = new global::MOUSE.Core.ActorDirectReplyHeader();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField0(result, (global::MOUSE.Core.Actors.ActorRef)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::MOUSE.Core.Actors.ActorRef), stream));
            return (global::MOUSE.Core.ActorDirectReplyHeader)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::MOUSE.Core.ActorDirectReplyHeader), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenMOUSE_Core_ActorDirectReplyHeaderSerializer()
        {
            Register();
        }
    }
}

namespace OrleansTestActor.Interface
{
    using global::Orleans.Async;
    using global::Orleans;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.SerializableAttribute, global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.GrainReferenceAttribute(typeof (global::OrleansTestActor.Interface.ITestGrain))]
    internal class OrleansCodeGenTestGrainReference : global::Orleans.Runtime.GrainReference, global::OrleansTestActor.Interface.ITestGrain
    {
        protected @OrleansCodeGenTestGrainReference(global::Orleans.Runtime.GrainReference @other): base (@other)
        {
        }

        protected @OrleansCodeGenTestGrainReference(global::System.Runtime.Serialization.SerializationInfo @info, global::System.Runtime.Serialization.StreamingContext @context): base (@info, @context)
        {
        }

        protected override global::System.Int32 InterfaceId
        {
            get
            {
                return 1273145025;
            }
        }

        public override global::System.String InterfaceName
        {
            get
            {
                return "global::OrleansTestActor.Interface.ITestGrain";
            }
        }

        public override global::System.Boolean @IsCompatible(global::System.Int32 @interfaceId)
        {
            return @interfaceId == 1273145025 || @interfaceId == -1277021679;
        }

        protected override global::System.String @GetMethodName(global::System.Int32 @interfaceId, global::System.Int32 @methodId)
        {
            switch (@interfaceId)
            {
                case 1273145025:
                    switch (@methodId)
                    {
                        case 46475815:
                            return "TestStateless";
                        case -1264203289:
                            return "TestStateful";
                        default:
                            throw new global::System.NotImplementedException("interfaceId=" + 1273145025 + ",methodId=" + @methodId);
                    }

                case -1277021679:
                    switch (@methodId)
                    {
                        default:
                            throw new global::System.NotImplementedException("interfaceId=" + -1277021679 + ",methodId=" + @methodId);
                    }

                default:
                    throw new global::System.NotImplementedException("interfaceId=" + @interfaceId);
            }
        }

        public global::System.Threading.Tasks.Task<global::MOUSE.Core.OperationResult> @TestStateless(global::PerfTests.Protocol.TestStateless @msg)
        {
            return base.@InvokeMethodAsync<global::MOUSE.Core.OperationResult>(46475815, new global::System.Object[]{@msg});
        }

        public global::System.Threading.Tasks.Task<global::MOUSE.Core.OperationResult> @TestStateful(global::PerfTests.Protocol.TestStateful @msg)
        {
            return base.@InvokeMethodAsync<global::MOUSE.Core.OperationResult>(-1264203289, new global::System.Object[]{@msg});
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::Orleans.CodeGeneration.MethodInvokerAttribute("global::OrleansTestActor.Interface.ITestGrain", 1273145025, typeof (global::OrleansTestActor.Interface.ITestGrain)), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    internal class OrleansCodeGenTestGrainMethodInvoker : global::Orleans.CodeGeneration.IGrainMethodInvoker
    {
        public global::System.Threading.Tasks.Task<global::System.Object> @Invoke(global::Orleans.Runtime.IAddressable @grain, global::Orleans.CodeGeneration.InvokeMethodRequest @request)
        {
            global::System.Int32 interfaceId = @request.@InterfaceId;
            global::System.Int32 methodId = @request.@MethodId;
            global::System.Object[] arguments = @request.@Arguments;
            try
            {
                if (@grain == null)
                    throw new global::System.ArgumentNullException("grain");
                switch (interfaceId)
                {
                    case 1273145025:
                        switch (methodId)
                        {
                            case 46475815:
                                return ((global::OrleansTestActor.Interface.ITestGrain)@grain).@TestStateless((global::PerfTests.Protocol.TestStateless)arguments[0]).@Box();
                            case -1264203289:
                                return ((global::OrleansTestActor.Interface.ITestGrain)@grain).@TestStateful((global::PerfTests.Protocol.TestStateful)arguments[0]).@Box();
                            default:
                                throw new global::System.NotImplementedException("interfaceId=" + 1273145025 + ",methodId=" + methodId);
                        }

                    case -1277021679:
                        switch (methodId)
                        {
                            default:
                                throw new global::System.NotImplementedException("interfaceId=" + -1277021679 + ",methodId=" + methodId);
                        }

                    default:
                        throw new global::System.NotImplementedException("interfaceId=" + interfaceId);
                }
            }
            catch (global::System.Exception exception)
            {
                return global::Orleans.Async.TaskUtility.@Faulted(exception);
            }
        }

        public global::System.Int32 InterfaceId
        {
            get
            {
                return 1273145025;
            }
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::PerfTests.Protocol.TestStateless)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenPerfTests_Protocol_TestStatelessSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field2 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField2 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field2);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField2 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field2);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::PerfTests.Protocol.TestStateless input = ((global::PerfTests.Protocol.TestStateless)original);
            global::PerfTests.Protocol.TestStateless result = new global::PerfTests.Protocol.TestStateless();
            result.@Data = (global::System.Collections.Generic.List<global::System.Int32>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(input.@Data);
            result.@SleepDurationMs = input.@SleepDurationMs;
            setField2(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField2(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::PerfTests.Protocol.TestStateless input = (global::PerfTests.Protocol.TestStateless)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@Data, stream, typeof (global::System.Collections.Generic.List<global::System.Int32>));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@SleepDurationMs, stream, typeof (global::System.Int32));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField2(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::PerfTests.Protocol.TestStateless result = new global::PerfTests.Protocol.TestStateless();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@Data = (global::System.Collections.Generic.List<global::System.Int32>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::System.Int32>), stream);
            result.@SleepDurationMs = (global::System.Int32)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Int32), stream);
            setField2(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::PerfTests.Protocol.TestStateless)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::PerfTests.Protocol.TestStateless), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenPerfTests_Protocol_TestStatelessSerializer()
        {
            Register();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.1.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::PerfTests.Protocol.TestStateful)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenPerfTests_Protocol_TestStatefulSerializer
    {
        private static readonly global::System.Reflection.FieldInfo field0 = typeof (global::MOUSE.Core.Message).@GetField("_headers", (System.@Reflection.@BindingFlags.@Instance | System.@Reflection.@BindingFlags.@NonPublic | System.@Reflection.@BindingFlags.@Public));
        private static readonly global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> getField0 = (global::System.Func<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetGetter(field0);
        private static readonly global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>> setField0 = (global::System.Action<global::MOUSE.Core.Message, global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>>)global::Orleans.Serialization.SerializationManager.@GetReferenceSetter(field0);
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::PerfTests.Protocol.TestStateful input = ((global::PerfTests.Protocol.TestStateful)original);
            global::PerfTests.Protocol.TestStateful result = new global::PerfTests.Protocol.TestStateful();
            setField0(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(getField0(input)));
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::PerfTests.Protocol.TestStateful input = (global::PerfTests.Protocol.TestStateful)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(getField0(input), stream, typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::PerfTests.Protocol.TestStateful result = new global::PerfTests.Protocol.TestStateful();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            setField0(result, (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Collections.Generic.List<global::MOUSE.Core.MessageHeader>), stream));
            return (global::PerfTests.Protocol.TestStateful)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::PerfTests.Protocol.TestStateful), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenPerfTests_Protocol_TestStatefulSerializer()
        {
            Register();
        }
    }
}
#pragma warning restore 162
#pragma warning restore 219
#pragma warning restore 414
#pragma warning restore 649
#pragma warning restore 693
#pragma warning restore 1591
#pragma warning restore 1998
#endif
