using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using NLog;


namespace MOUSE.Core
{
    public class FastSerializer
    {
        #region Prototype
        private interface ISerializationPrototype
        {
            void Serialize(object obj, NativeWriter writer);
            object Deserialize(NativeReader reader);

            Type Type { get; }
            ushort TypeKey { get; }
        }

        private class SerializationPrototypeNull : ISerializationPrototype
        {
            protected readonly ushort _typeKey;

            public SerializationPrototypeNull(ushort typeKey)
            {
                _typeKey = typeKey;
            }

            public void Serialize(object obj, NativeWriter writer)
            {
            }

            public object Deserialize(NativeReader reader)
            {
                return null;
            }

            public Type Type
            {
                get { throw new NullReferenceException("Can't get type for null refernece."); }
            }

            public ushort TypeKey
            {
                get { return _typeKey; }
            }
        }

        private class SerializationPrototypeArrayByte : ISerializationPrototype
        {
            protected readonly Type _type;
            protected readonly ushort _typeKey;

            public SerializationPrototypeArrayByte(ushort typeKey)
            {
                _type = typeof(Byte[]);
                _typeKey = typeKey;
            }

            public void Serialize(object obj, NativeWriter writer)
            {
                try
                {
                    Byte[] array = (Byte[])obj;

                    writer.Write(array.Length);
                    for (int i = 0, length = array.Length; i < length; i++)
                        writer.Write(array[i]);
                }
                catch
                {
                    Logger.Error("Can't serialize Byte[] type.");
                    throw;
                }
            }

            public object Deserialize(NativeReader reader)
            {
                try
                {
                    Byte[] array = new Byte[reader.ReadInt32()];

                    for (int i = 0, length = array.Length; i < length; i++)
                        array[i] = reader.ReadByte();

                    return array;
                }
                catch
                {
                    Logger.Error("Can't deserialize Byte[] type.");
                    throw;
                }
            }

            public Type Type
            {
                get { return _type; }
            }

            public ushort TypeKey
            {
                get { return _typeKey; }
            }
        }

        private class SerializationPrototype : ISerializationPrototype
        {
            #region Field descriptions
            protected delegate void SetterDelegateReference(object obj, object val);
            protected delegate object GetterDelegateReference(object obj);

            protected static SetterDelegateReference GetSetterReference(Type ownerType, FieldInfo fieldInfo)
            {
                DynamicMethod setterMethod = new DynamicMethod("", typeof(void), new Type[] { typeof(object), typeof(object) }, ownerType);
                ILGenerator setterGen = setterMethod.GetILGenerator();

                setterGen.Emit(OpCodes.Ldarg_0);
                setterGen.Emit(OpCodes.Castclass, ownerType);
                setterGen.Emit(OpCodes.Ldarg_1);
                setterGen.Emit(OpCodes.Castclass, fieldInfo.FieldType);
                setterGen.Emit(OpCodes.Stfld, fieldInfo);
                setterGen.Emit(OpCodes.Ret);

                return (SetterDelegateReference)setterMethod.CreateDelegate(typeof(SetterDelegateReference));
            }

            protected static GetterDelegateReference GetGetterReference(Type ownerType, FieldInfo fieldInfo)
            {
                DynamicMethod getterMethod = new DynamicMethod("", typeof(object), new Type[] { typeof(object) }, ownerType);
                ILGenerator getterGen = getterMethod.GetILGenerator();

                getterGen.Emit(OpCodes.Ldarg_0);
                getterGen.Emit(OpCodes.Castclass, ownerType);
                getterGen.Emit(OpCodes.Ldfld, fieldInfo);
                getterGen.Emit(OpCodes.Castclass, fieldInfo.FieldType);
                getterGen.Emit(OpCodes.Ret);

                return (GetterDelegateReference)getterMethod.CreateDelegate(typeof(GetterDelegateReference));
            }

            private interface IField
            {
                bool SerializeFrom(object obj, NativeWriter writer);
                bool Serialize(object obj, NativeWriter writer);

                void DeserializeTo(object obj, NativeReader reader);
                object Deserialize(NativeReader reader);

                byte Id { get; }
            }

            private class Field : IField
            {
                #region Field types
                public enum FieldType
                {
                    Boolean = 0,

                    Enum,

                    Byte,
                    SByte,
                    Int16,
                    UInt16,
                    Int32,
                    UInt32,
                    Int64,
                    UInt64,

                    Single,
                    Double,

                    String,

                    DateTime,

                    Array,
                    List,
                    Dictionary,

                    Struct,
                    Class,
                }
                #endregion

                #region Setter Delegates
                private delegate void SetterDelegate<T>(object obj, T val);
                #endregion

                #region Getter Delegates
                private delegate T GetterDelegate<T>(object obj);
                #endregion

                #region Create setters

                private static object GetSetter(Type ownerType, FieldType fieldType, FieldInfo fieldInfo)
                {
                    Type param2Type = fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType == typeof(String) ?
                                                 fieldInfo.FieldType :
                                                (fieldInfo.FieldType.IsEnum ? typeof(UInt32) : typeof(object));

                    DynamicMethod setterMethod = new DynamicMethod("", typeof(void), new Type[] { typeof(object), param2Type },
                                                                   ownerType);
                    ILGenerator setterGen = setterMethod.GetILGenerator();

                    setterGen.Emit(OpCodes.Ldarg_0);
                    setterGen.Emit(OpCodes.Castclass, ownerType);
                    setterGen.Emit(OpCodes.Ldarg_1);
                    setterGen.Emit(OpCodes.Stfld, fieldInfo);
                    setterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Byte>));
                        case FieldType.SByte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<SByte>));
                        case FieldType.Int16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt64>));
                        case FieldType.Single:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Single>));
                        case FieldType.Double:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Double>));
                        case FieldType.String:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<String>));
                        case FieldType.Class:
                            return setterMethod.CreateDelegate(typeof(SetterDelegateReference));
                        default:
                            throw new NotSupportedException(String.Format("Can't create set delegate for {0} type and {1} field .",
                                ownerType, fieldInfo));
                    }
                }
                #endregion

                #region Create getters
                private static object GetGetter(Type ownerType, FieldType fieldType, FieldInfo fieldInfo)
                {
                    Type returnType = fieldInfo.FieldType.IsPrimitive || fieldInfo.FieldType == typeof(String) ?
                                                 fieldInfo.FieldType :
                                                (fieldInfo.FieldType.IsEnum ? typeof(UInt32) : typeof(object));

                    DynamicMethod getterMethod = new DynamicMethod("", returnType, new Type[] { typeof(object) }, ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Castclass, ownerType);
                    getterGen.Emit(OpCodes.Ldfld, fieldInfo);
                    getterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Byte>));
                        case FieldType.SByte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<SByte>));
                        case FieldType.Int16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt64>));
                        case FieldType.Single:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Single>));
                        case FieldType.Double:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Double>));
                        case FieldType.String:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<String>));
                        case FieldType.Class:
                            return getterMethod.CreateDelegate(typeof(GetterDelegateReference));
                        default:
                            throw new NotSupportedException(String.Format("Can't create get delegate for {0} type and {1} field .",
                                ownerType, fieldInfo));
                    }
                }
                #endregion

                private readonly byte _id;

                protected readonly Type _fieldType;
                protected readonly uint _fieldTypeKey;

                protected readonly Type _ownerType;

                protected readonly FieldType _fieldTypeId = FieldType.Class;

                private readonly object _setter;
                private readonly object _getter;

                protected readonly bool _writeDefault = false;

                protected Field(byte id, Type ownerType, Type fieldType, bool writeDefault)
                {
                    CheckPrototypeDefined(fieldType);

                    _id = id;

                    _ownerType = ownerType;

                    _fieldType = fieldType;
                    _fieldTypeKey = _typeToKey[fieldType];

                    _fieldTypeId = GetFieldType(fieldType);

                    _writeDefault = writeDefault;
                }

                public Field(byte id, Type ownerType, FieldInfo fieldInfo, bool writeDefault)
                    : this(id, ownerType, fieldInfo.FieldType, writeDefault)
                {
                    _setter = GetSetter(ownerType, _fieldTypeId, fieldInfo);
                    _getter = GetGetter(ownerType, _fieldTypeId, fieldInfo);
                }

                #region Field serialization
                public virtual bool Serialize(object obj, NativeWriter writer)
                {
                    throw new NotImplementedException();
                }

                public virtual bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        switch (_fieldTypeId)
                        {
                            case FieldType.Boolean:
                                {
                                    Boolean value = ((GetterDelegate<Boolean>)_getter)(obj);
                                    if (_writeDefault || value)
                                        writer.Write(value);
                                    return _writeDefault || value;
                                }

                            case FieldType.Enum:
                                {
                                    UInt32 value = ((GetterDelegate<UInt32>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Byte:
                                {
                                    Byte value = ((GetterDelegate<Byte>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.SByte:
                                {
                                    SByte value = ((GetterDelegate<SByte>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Int16:
                                {
                                    Int16 value = ((GetterDelegate<Int16>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.UInt16:
                                {
                                    UInt16 value = ((GetterDelegate<UInt16>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Int32:
                                {
                                    Int32 value = ((GetterDelegate<Int32>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.UInt32:
                                {
                                    UInt32 value = ((GetterDelegate<UInt32>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Int64:
                                {
                                    Int64 value = ((GetterDelegate<Int64>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.UInt64:
                                {
                                    UInt64 value = ((GetterDelegate<UInt64>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Single:
                                {
                                    Single value = ((GetterDelegate<Single>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.Double:
                                {
                                    Double value = ((GetterDelegate<Double>)_getter)(obj);
                                    if (_writeDefault || value != 0)
                                        writer.Write(value);
                                    return _writeDefault || value != 0;
                                }

                            case FieldType.String:
                                {
                                    String value = ((GetterDelegate<String>)_getter)(obj);
                                    if (value == null)
                                    {
                                        if (_writeDefault)
                                            writer.Write(0);

                                        return _writeDefault;
                                    }

                                    writer.WriteUnicode(value);
                                    return true;
                                }

                            case FieldType.DateTime:
                                writer.Write(((GetterDelegate<DateTime>)_getter)(obj).Ticks);
                                return true;

                            case FieldType.Class:
                                {
                                    object fieldValue = ((GetterDelegateReference)_getter)(obj);

                                    // тут постоянно в рантайме будем определять тип для поддержки полиморфизма
                                    ushort fieldTypeKey = fieldValue != null ? _typeToKey[fieldValue.GetType()] : _nullTypeKey;

                                    ISerializationPrototype prototype;
                                    if (!_deserializationPrototypes.TryGetValue(fieldTypeKey, out prototype))
                                        throw new NotSupportedException(String.Format("Can't serialize {0} type.",
                                                                        fieldValue != null ? fieldValue.GetType().ToString() : "<null>"));

                                    writer.Write(fieldTypeKey);
                                    prototype.Serialize(fieldValue, writer);
                                    return true;
                                }

                            default:
                                throw new NotSupportedException(String.Format("Can't serialize {0} field.", _fieldTypeId));
                        }
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s {1} field.", _ownerType, _fieldType);
                        throw;
                    }
                }
                #endregion

                #region Field deserialization
                public virtual object Deserialize(NativeReader reader)
                {
                    throw new NotImplementedException();
                }

                public virtual void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        switch (_fieldTypeId)
                        {
                            case FieldType.Boolean:
                                ((SetterDelegate<Boolean>)_setter)(obj, reader.ReadBoolean());
                                break;

                            case FieldType.Enum:
                                ((SetterDelegate<UInt32>)_setter)(obj, reader.ReadUInt32());
                                break;

                            case FieldType.Byte:
                                ((SetterDelegate<Byte>)_setter)(obj, reader.ReadByte());
                                break;

                            case FieldType.SByte:
                                ((SetterDelegate<SByte>)_setter)(obj, reader.ReadSByte());
                                break;

                            case FieldType.Int16:
                                ((SetterDelegate<Int16>)_setter)(obj, reader.ReadInt16());
                                break;

                            case FieldType.UInt16:
                                ((SetterDelegate<UInt16>)_setter)(obj, reader.ReadUInt16());
                                break;

                            case FieldType.Int32:
                                ((SetterDelegate<Int32>)_setter)(obj, reader.ReadInt32());
                                break;

                            case FieldType.UInt32:
                                ((SetterDelegate<UInt32>)_setter)(obj, reader.ReadUInt32());
                                break;

                            case FieldType.Int64:
                                ((SetterDelegate<Int64>)_setter)(obj, reader.ReadInt64());
                                break;

                            case FieldType.UInt64:
                                ((SetterDelegate<UInt64>)_setter)(obj, reader.ReadUInt64());
                                break;

                            case FieldType.Single:
                                ((SetterDelegate<Single>)_setter)(obj, reader.ReadSingle());
                                break;

                            case FieldType.Double:
                                ((SetterDelegate<Double>)_setter)(obj, reader.ReadDouble());
                                break;

                            case FieldType.String:
                                ((SetterDelegate<String>)_setter)(obj, reader.ReadUnicode());
                                break;

                            case FieldType.DateTime:
                                ((SetterDelegate<DateTime>)_setter)(obj, new DateTime(reader.ReadInt64()));
                                break;

                            case FieldType.Class:
                                {
                                    ushort typeKey = reader.ReadUInt16();

                                    ISerializationPrototype prototype;
                                    if (!_deserializationPrototypes.TryGetValue(typeKey, out prototype))
                                        throw new NotSupportedException(String.Format("Can't deserialize for {0} type key.", typeKey));

                                    ((SetterDelegateReference)_setter)(obj, prototype.Deserialize(reader));
                                }
                                break;

                            default:
                                throw new NotSupportedException(String.Format("Can't deserialize {0} field.", _fieldTypeId));
                        }
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s {1} field.", _ownerType, _fieldType);
                        throw;
                    }
                }
                #endregion

                #region Get field type
                public static FieldType GetFieldType(Type fieldType)
                {
                    if (fieldType == typeof(Boolean))
                        return FieldType.Boolean;
                    if (fieldType == typeof(Byte))
                        return FieldType.Byte;
                    if (fieldType == typeof(SByte))
                        return FieldType.SByte;
                    if (fieldType == typeof(Int16))
                        return FieldType.Int16;
                    if (fieldType == typeof(UInt16))
                        return FieldType.UInt16;
                    if (fieldType == typeof(Int32))
                        return FieldType.Int32;
                    if (fieldType == typeof(UInt32))
                        return FieldType.UInt32;
                    if (fieldType == typeof(Int64))
                        return FieldType.Int64;
                    if (fieldType == typeof(UInt64))
                        return FieldType.UInt64;
                    if (fieldType == typeof(Single))
                        return FieldType.Single;
                    if (fieldType == typeof(Double))
                        return FieldType.Double;
                    if (fieldType == typeof(String))
                        return FieldType.String;
                    if (fieldType == typeof(DateTime))
                        return FieldType.DateTime;

                    if (fieldType.IsPrimitive)
                        throw new NotSupportedException(String.Format("Unexpexted primitive type {0}.", fieldType));

                    if (fieldType.IsArray)
                        return FieldType.Array;
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(List<>)))
                        return FieldType.List;
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(KeyValueCollection<,>)))
                        return FieldType.Dictionary;

                    if (fieldType.IsEnum)
                        return FieldType.Enum;
                    if (fieldType.IsValueType)
                        return FieldType.Struct;

                    return FieldType.Class;
                }
                #endregion

                public byte Id
                {
                    get { return _id; }
                }
            }

            private class FieldNullable : Field
            {
                #region Setter Delegates
                private delegate void SetterDelegate<T>(object obj, T value)
                    where T : struct;
                #endregion

                #region Getter Delegates
                private delegate Boolean CheckDelegate(object obj);
                private delegate T GetterDelegate<T>(object obj)
                    where T : struct;
                #endregion

                #region Create setters
                private static object GetSetterDelegate(Type ownerType, FieldType fieldType, FieldInfo fieldInfo)
                {
                    Type valueType = fieldInfo.FieldType.GetGenericArguments()[0];

                    DynamicMethod setterMethod = new DynamicMethod("", typeof(void), new Type[] { typeof(object), valueType }, ownerType);
                    ILGenerator setterGen = setterMethod.GetILGenerator();

                    setterGen.Emit(OpCodes.Ldarg_0);
                    setterGen.Emit(OpCodes.Castclass, ownerType);
                    setterGen.Emit(OpCodes.Ldarg_1);
                    setterGen.Emit(OpCodes.Newobj, fieldInfo.FieldType.GetConstructor(new Type[] { valueType }));
                    setterGen.Emit(OpCodes.Stfld, fieldInfo);
                    setterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Byte>));
                        case FieldType.SByte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<SByte>));
                        case FieldType.Int16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt64>));
                        case FieldType.Single:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Single>));
                        case FieldType.Double:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Double>));
                        case FieldType.DateTime:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<DateTime>));
                        case FieldType.Struct:
                            return setterMethod.CreateDelegate(typeof(SetterDelegateReference));
                        default:
                            throw new NotSupportedException(String.Format("Can't create set delegate for Nullable<{0}>, owner {1}.",
                                valueType, ownerType));
                    }
                }
                #endregion

                #region Create getters
                private static CheckDelegate GetCheckDelegate(Type ownerType, FieldInfo fieldInfo)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", typeof(Boolean), new Type[] { typeof(object) }, ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Castclass, ownerType);
                    getterGen.Emit(OpCodes.Ldflda, fieldInfo);
                    getterGen.Emit(OpCodes.Call, fieldInfo.FieldType.GetProperty("HasValue").GetGetMethod());
                    getterGen.Emit(OpCodes.Ret);

                    return (CheckDelegate)getterMethod.CreateDelegate(typeof(CheckDelegate));
                }

                private static object GetGetterDelegate(Type ownerType, FieldType fieldType, FieldInfo fieldInfo)
                {
                    Type valueType = fieldInfo.FieldType.GetGenericArguments()[0];

                    DynamicMethod getterMethod = new DynamicMethod("", valueType, new Type[] { typeof(object) }, ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Castclass, ownerType);
                    getterGen.Emit(OpCodes.Ldflda, fieldInfo);
                    getterGen.Emit(OpCodes.Call, fieldInfo.FieldType.GetProperty("Value").GetGetMethod());
                    getterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Byte>));
                        case FieldType.SByte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<SByte>));
                        case FieldType.Int16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt64>));
                        case FieldType.Single:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Single>));
                        case FieldType.Double:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Double>));
                        case FieldType.DateTime:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<DateTime>));
                        case FieldType.Struct:
                            return getterMethod.CreateDelegate(typeof(GetterDelegateReference));
                        default:
                            throw new NotSupportedException(String.Format("Can't create get delegate for Nullable<{0}>, owner {1}.",
                                valueType, ownerType));
                    }
                }
                #endregion

                private readonly CheckDelegate _hasValue;
                private readonly object _setter;
                private readonly object _getter;

                public FieldNullable(byte id, Type ownerType, FieldInfo fieldInfo)
                    : base(id, ownerType, fieldInfo.FieldType.GetGenericArguments()[0], false)
                {
                    _hasValue = GetCheckDelegate(ownerType, fieldInfo);
                    _setter = GetSetterDelegate(ownerType, _fieldTypeId, fieldInfo);
                    _getter = GetGetterDelegate(ownerType, _fieldTypeId, fieldInfo);
                }

                #region Field serialization
                public override bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        if (!_writeDefault && !_hasValue(obj))
                            return false;

                        switch (_fieldTypeId)
                        {
                            case FieldType.Boolean:
                                writer.Write(((GetterDelegate<Boolean>)_getter)(obj));
                                break;

                            case FieldType.Enum:
                                writer.Write(((GetterDelegate<UInt32>)_getter)(obj));
                                break;

                            case FieldType.Byte:
                                writer.Write(((GetterDelegate<Byte>)_getter)(obj));
                                break;

                            case FieldType.SByte:
                                writer.Write(((GetterDelegate<SByte>)_getter)(obj));
                                break;

                            case FieldType.Int16:
                                writer.Write(((GetterDelegate<Int16>)_getter)(obj));
                                break;

                            case FieldType.UInt16:
                                writer.Write(((GetterDelegate<UInt16>)_getter)(obj));
                                break;

                            case FieldType.Int32:
                                writer.Write(((GetterDelegate<Int32>)_getter)(obj));
                                break;

                            case FieldType.UInt32:
                                writer.Write(((GetterDelegate<UInt32>)_getter)(obj));
                                break;

                            case FieldType.Int64:
                                writer.Write(((GetterDelegate<Int64>)_getter)(obj));
                                break;

                            case FieldType.UInt64:
                                writer.Write(((GetterDelegate<UInt64>)_getter)(obj));
                                break;

                            case FieldType.Single:
                                writer.Write(((GetterDelegate<Single>)_getter)(obj));
                                break;

                            case FieldType.Double:
                                writer.Write(((GetterDelegate<Double>)_getter)(obj));
                                break;

                            case FieldType.DateTime:
                                writer.Write(((GetterDelegate<DateTime>)_getter)(obj).Ticks);
                                break;

                            default:
                                throw new NotSupportedException(String.Format("Can't serialize {0} field.", _fieldTypeId));
                        }

                        return true;
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s Nullable<{1}> field.", _ownerType,
                                     _fieldType);
                        throw;
                    }
                }
                #endregion

                #region Field deserialization
                public override void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        switch (_fieldTypeId)
                        {
                            case FieldType.Boolean:
                                ((SetterDelegate<Boolean>)_setter)(obj, reader.ReadBoolean());
                                break;

                            case FieldType.Enum:
                                ((SetterDelegate<UInt32>)_setter)(obj, reader.ReadUInt32());
                                break;

                            case FieldType.Byte:
                                ((SetterDelegate<Byte>)_setter)(obj, reader.ReadByte());
                                break;

                            case FieldType.SByte:
                                ((SetterDelegate<SByte>)_setter)(obj, reader.ReadSByte());
                                break;

                            case FieldType.Int16:
                                ((SetterDelegate<Int16>)_setter)(obj, reader.ReadInt16());
                                break;

                            case FieldType.UInt16:
                                ((SetterDelegate<UInt16>)_setter)(obj, reader.ReadUInt16());
                                break;

                            case FieldType.Int32:
                                ((SetterDelegate<Int32>)_setter)(obj, reader.ReadInt32());
                                break;

                            case FieldType.UInt32:
                                ((SetterDelegate<UInt32>)_setter)(obj, reader.ReadUInt32());
                                break;

                            case FieldType.Int64:
                                ((SetterDelegate<Int64>)_setter)(obj, reader.ReadInt64());
                                break;

                            case FieldType.UInt64:
                                ((SetterDelegate<UInt64>)_setter)(obj, reader.ReadUInt64());
                                break;

                            case FieldType.Single:
                                ((SetterDelegate<Single>)_setter)(obj, reader.ReadSingle());
                                break;

                            case FieldType.Double:
                                ((SetterDelegate<Double>)_setter)(obj, reader.ReadDouble());
                                break;

                            case FieldType.DateTime:
                                ((SetterDelegate<DateTime>)_setter)(obj, new DateTime(reader.ReadInt64()));
                                break;

                            default:
                                throw new NotSupportedException(String.Format("Can't deserialize {0} field.", _fieldTypeId));
                        }
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s Nullable<{1}> field.", _ownerType, _fieldType);
                        throw;
                    }
                }
                #endregion
            }

            private class FieldDateTime : Field
            {
                #region Setter Delegates
                private delegate void SetterDelegate(object obj, DateTime val);
                #endregion

                #region Getter Delegates
                private delegate DateTime GetterDelegate(object obj);
                #endregion

                #region Create setters
                private static SetterDelegate GetSetter(Type ownerType, FieldInfo fieldInfo)
                {
                    DynamicMethod setterMethod = new DynamicMethod("", typeof(void),
                                                                   new Type[] { typeof(object), typeof(DateTime) },
                                                                   ownerType);
                    ILGenerator setterGen = setterMethod.GetILGenerator();

                    setterGen.Emit(OpCodes.Ldarg_0);
                    setterGen.Emit(OpCodes.Castclass, ownerType);
                    setterGen.Emit(OpCodes.Ldarg_1);
                    setterGen.Emit(OpCodes.Stfld, fieldInfo);
                    setterGen.Emit(OpCodes.Ret);

                    return (SetterDelegate)setterMethod.CreateDelegate(typeof(SetterDelegate));
                }
                #endregion

                #region Create getters
                private static GetterDelegate GetGetter(Type ownerType, FieldInfo fieldInfo)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", typeof(DateTime), new Type[] { typeof(object) }, ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Castclass, ownerType);
                    getterGen.Emit(OpCodes.Ldfld, fieldInfo);
                    getterGen.Emit(OpCodes.Ret);

                    return (GetterDelegate)getterMethod.CreateDelegate(typeof(GetterDelegate));
                }
                #endregion

                private readonly SetterDelegate _setter;
                private readonly GetterDelegate _getter;

                public FieldDateTime(byte id, Type ownerType, FieldInfo fieldInfo)
                    : base(id, ownerType, typeof(DateTime), false)
                {
                    _setter = GetSetter(ownerType, fieldInfo);
                    _getter = GetGetter(ownerType, fieldInfo);
                }

                public override bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        writer.Write(_getter(obj).Ticks);
                        return true;
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s DateTime field.", _ownerType);
                        throw;
                    }
                }

                public override void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        _setter(obj, new DateTime(reader.ReadInt64()));
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s DateTime field.", _ownerType);
                        throw;
                    }
                }
            }

            private class FieldArray : Field
            {
                #region Constructor Delegate
                protected delegate Array CreateObjectDelegate(int capacity);
                #endregion

                #region Setter Delegates
                private delegate void SetterDelegate<T>(T[] array, int index, T val);
                #endregion

                #region Getter Delegates
                private delegate T GetterDelegate<T>(T[] array, int index);
                #endregion

                #region Create constructor
                private static CreateObjectDelegate GetConstructorDelegate(Type ownerType, Type elementType)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", elementType.MakeArrayType(), new Type[] { typeof(int) }, elementType);
                    ILGenerator constructorGen = getterMethod.GetILGenerator();

                    constructorGen.Emit(OpCodes.Ldarg_0);
                    constructorGen.Emit(OpCodes.Newarr, elementType);
                    constructorGen.Emit(OpCodes.Castclass, elementType.MakeArrayType());
                    constructorGen.Emit(OpCodes.Ret);

                    return (CreateObjectDelegate)getterMethod.CreateDelegate(typeof(CreateObjectDelegate));
                }
                #endregion

                #region Create setters
                private static object GetSetterElement(Type ownerType, FieldType fieldType, Type elementType)
                {
                    Type paramType = elementType.IsPrimitive || elementType == typeof(String) || elementType == typeof(DateTime) ?
                                        elementType : (elementType.IsEnum ? typeof(UInt32) : typeof(object));

                    DynamicMethod setterMethod = new DynamicMethod("", typeof(void), new Type[] { typeof(object), 
                                                                                                  typeof(Int32), 
                                                                                                  paramType },
                                                                   ownerType);
                    ILGenerator setterGen = setterMethod.GetILGenerator();

                    setterGen.Emit(OpCodes.Ldarg_0);
                    setterGen.Emit(OpCodes.Ldarg_1);
                    setterGen.Emit(OpCodes.Ldarg_2);
                    setterGen.Emit(OpCodes.Stelem, elementType);
                    setterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Byte>));
                        case FieldType.SByte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<SByte>));
                        case FieldType.Int16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt64>));
                        case FieldType.Single:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Single>));
                        case FieldType.Double:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Double>));
                        case FieldType.DateTime:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<DateTime>));
                        case FieldType.String:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<String>));
                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                        case FieldType.Class:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Object>));
                        default:
                            throw new NotSupportedException(String.Format("Can't create set delegate for {0}[], owner {1}.", elementType, ownerType));
                    }
                }
                #endregion

                #region Create getters
                private static object GetGetterElement(Type ownerType, FieldType fieldType, Type elementType)
                {
                    Type returnType = elementType.IsPrimitive || elementType == typeof(String) || elementType == typeof(DateTime) ?
                                        elementType : (elementType.IsEnum ? typeof(UInt32) : typeof(object));

                    DynamicMethod getterMethod = new DynamicMethod("", returnType,
                                                                   new Type[] { typeof(object), typeof(Int32) },
                                                                   ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Ldarg_1);
                    getterGen.Emit(OpCodes.Ldelem, returnType);
                    getterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Byte>));
                        case FieldType.SByte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<SByte>));
                        case FieldType.Int16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt64>));
                        case FieldType.Single:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Single>));
                        case FieldType.Double:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Double>));
                        case FieldType.DateTime:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<DateTime>));
                        case FieldType.String:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<String>));
                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                        case FieldType.Class:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Object>));
                        default:
                            throw new NotSupportedException(String.Format("Can't create get delegate for {0}[], owner {1}.", elementType, ownerType));
                    }
                }
                #endregion

                private readonly IField _referenceField;

                private readonly CreateObjectDelegate _arrayConstructor;

                private readonly object _elementSetter;
                private readonly object _elementGetter;

                private readonly SetterDelegateReference _arraySetter;
                private readonly GetterDelegateReference _arrayGetter;

                public FieldArray(byte id, Type ownerType, Type elementType, bool writeDefault)
                    : base(id, ownerType, elementType, writeDefault)
                {
                    CheckPrototypeDefined(elementType);

                    if (_fieldTypeId == FieldType.List)
                        _referenceField = new FieldList(0, ownerType, elementType.GetGenericArguments()[0], true);
                    if (_fieldTypeId == FieldType.Array)
                        _referenceField = new FieldArray(0, ownerType, elementType.GetElementType(), true);
                    if (_fieldTypeId == FieldType.Dictionary)
                        _referenceField = new FieldDictionary(0, ownerType, elementType.GetGenericArguments()[0],
                                                                            elementType.GetGenericArguments()[1],
                                                                            true);

                    _arrayConstructor = GetConstructorDelegate(ownerType, elementType);

                    _elementSetter = GetSetterElement(ownerType, _fieldTypeId, elementType);
                    _elementGetter = GetGetterElement(ownerType, _fieldTypeId, elementType);
                }

                public FieldArray(byte id, Type ownerType, FieldInfo info, bool writeDefault)
                    : this(id, ownerType, info.FieldType.GetElementType(), writeDefault)
                {
                    _arraySetter = GetSetterReference(ownerType, info);
                    _arrayGetter = GetGetterReference(ownerType, info);
                }

                public override bool Serialize(object obj, NativeWriter writer)
                {
                    try
                    {
                        Array array = (Array)obj;
                        if (array == null)
                        {
                            if (_writeDefault)
                                writer.Write(-1);

                            return _writeDefault;
                        }

                        writer.Write(array.Length);
                        for (int i = 0, count = array.Length; i < count; i++)
                            SerializeElement(array, i, writer);

                        return true;
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s {1}[] field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                public override bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        return Serialize(_arrayGetter(obj), writer);
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s {1}[] field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                #region SerializeFrom array element
                private void SerializeElement(Array array, int index, NativeWriter writer)
                {
                    switch (_fieldTypeId)
                    {
                        case FieldType.Boolean:
                            writer.Write(((GetterDelegate<Boolean>)_elementGetter)((Boolean[])array, index));
                            break;

                        case FieldType.Enum:
                            writer.Write(((GetterDelegate<UInt32>)_elementGetter)((UInt32[])array, index));
                            break;

                        case FieldType.Byte:
                            writer.Write(((GetterDelegate<Byte>)_elementGetter)((Byte[])array, index));
                            break;

                        case FieldType.SByte:
                            writer.Write(((GetterDelegate<SByte>)_elementGetter)((SByte[])array, index));
                            break;

                        case FieldType.Int16:
                            writer.Write(((GetterDelegate<Int16>)_elementGetter)((Int16[])array, index));
                            break;

                        case FieldType.UInt16:
                            writer.Write(((GetterDelegate<UInt16>)_elementGetter)((UInt16[])array, index));
                            break;

                        case FieldType.Int32:
                            writer.Write(((GetterDelegate<Int32>)_elementGetter)((Int32[])array, index));
                            break;

                        case FieldType.UInt32:
                            writer.Write(((GetterDelegate<UInt32>)_elementGetter)((UInt32[])array, index));
                            break;

                        case FieldType.Int64:
                            writer.Write(((GetterDelegate<Int64>)_elementGetter)((Int64[])array, index));
                            break;

                        case FieldType.UInt64:
                            writer.Write(((GetterDelegate<UInt64>)_elementGetter)((UInt64[])array, index));
                            break;

                        case FieldType.Single:
                            writer.Write(((GetterDelegate<Single>)_elementGetter)((Single[])array, index));
                            break;

                        case FieldType.Double:
                            writer.Write(((GetterDelegate<Double>)_elementGetter)((Double[])array, index));
                            break;

                        case FieldType.DateTime:
                            writer.Write(((GetterDelegate<DateTime>)_elementGetter)((DateTime[])array, index).Ticks);
                            break;

                        case FieldType.String:
                            writer.WriteUnicode(((GetterDelegate<String>)_elementGetter)((String[])array, index));
                            break;

                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                            _referenceField.Serialize(array.GetValue(index), writer);
                            break;

                        case FieldType.Class:
                            {
                                object fieldValue = array.GetValue(index);

                                // тут постоянно в рантайме будем определять тип для поддержки полиморфизма
                                ushort fieldTypeKey = fieldValue != null ? _typeToKey[fieldValue.GetType()] : _nullTypeKey;

                                ISerializationPrototype prototype;
                                if (!_deserializationPrototypes.TryGetValue(fieldTypeKey, out prototype))
                                    throw new NotSupportedException(String.Format("Can't serialize {0} type.",
                                                                    fieldValue != null ? fieldValue.GetType().ToString() : "<null>"));

                                writer.Write(fieldTypeKey);
                                prototype.Serialize(fieldValue, writer);
                            }
                            break;

                        default:
                            throw new NotSupportedException(String.Format("Can't serialize {0} field.", _fieldTypeId));
                    }
                }
                #endregion

                public override void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        _arraySetter(obj, Deserialize(reader));
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s {1}[] field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                public override object Deserialize(NativeReader reader)
                {
                    try
                    {
                        int length = reader.ReadInt32();

                        Array array = length > -1 ? _arrayConstructor(length) : null;
                        for (int i = 0; i < length; i++)
                            DeserializeElement(array, i, reader);

                        return array;
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s {1}[] field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                #region DeserializeTo array element
                private void DeserializeElement(Array array, int index, NativeReader reader)
                {
                    switch (_fieldTypeId)
                    {
                        case FieldType.Boolean:
                            ((SetterDelegate<Boolean>)_elementSetter)((Boolean[])array, index, reader.ReadBoolean());
                            break;

                        case FieldType.Enum:
                            ((SetterDelegate<UInt32>)_elementSetter)((UInt32[])array, index, reader.ReadUInt32());
                            break;

                        case FieldType.Byte:
                            ((SetterDelegate<Byte>)_elementSetter)((Byte[])array, index, reader.ReadByte());
                            break;

                        case FieldType.SByte:
                            ((SetterDelegate<SByte>)_elementSetter)((SByte[])array, index, reader.ReadSByte());
                            break;

                        case FieldType.Int16:
                            ((SetterDelegate<Int16>)_elementSetter)((Int16[])array, index, reader.ReadInt16());
                            break;

                        case FieldType.UInt16:
                            ((SetterDelegate<UInt16>)_elementSetter)((UInt16[])array, index, reader.ReadUInt16());
                            break;

                        case FieldType.Int32:
                            ((SetterDelegate<Int32>)_elementSetter)((Int32[])array, index, reader.ReadInt32());
                            break;

                        case FieldType.UInt32:
                            ((SetterDelegate<UInt32>)_elementSetter)((UInt32[])array, index, reader.ReadUInt32());
                            break;

                        case FieldType.Int64:
                            ((SetterDelegate<Int64>)_elementSetter)((Int64[])array, index, reader.ReadInt64());
                            break;

                        case FieldType.UInt64:
                            ((SetterDelegate<UInt64>)_elementSetter)((UInt64[])array, index, reader.ReadUInt64());
                            break;

                        case FieldType.Single:
                            ((SetterDelegate<Single>)_elementSetter)((Single[])array, index, reader.ReadSingle());
                            break;

                        case FieldType.Double:
                            ((SetterDelegate<Double>)_elementSetter)((Double[])array, index, reader.ReadDouble());
                            break;

                        case FieldType.DateTime:
                            ((SetterDelegate<DateTime>)_elementSetter)((DateTime[])array, index, new DateTime(reader.ReadInt64()));
                            break;

                        case FieldType.String:
                            ((SetterDelegate<String>)_elementSetter)((String[])array, index, reader.ReadUnicode());
                            break;

                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                            array.SetValue(_referenceField.Deserialize(reader), index);
                            break;

                        case FieldType.Class:
                            {
                                ushort typeKey = reader.ReadUInt16();

                                ISerializationPrototype prototype;
                                if (!_deserializationPrototypes.TryGetValue(typeKey, out prototype))
                                    throw new NotSupportedException(String.Format("Can't deserialize for {0} type key.", typeKey));

                                array.SetValue(prototype.Deserialize(reader), index);
                            }
                            break;

                        default:
                            throw new NotSupportedException(String.Format("Can't deserialize {0} field.", _fieldTypeId));
                    }
                }
                #endregion
            }

            private class FieldList : Field
            {
                #region Constructor Delegate
                protected delegate IList CreateListDelegate(int capacity);
                #endregion

                #region Setter Delegates
                private delegate void SetterDelegate<T>(List<T> array, int index, T val);
                #endregion

                #region Getter Delegates
                private delegate T GetterDelegate<T>(List<T> array, int index);
                #endregion

                #region Create constructor
                private static CreateListDelegate GetConstructorDelegate(Type ownerType, Type listType)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", listType, new Type[] { typeof(int) }, ownerType);
                    ILGenerator constructorGen = getterMethod.GetILGenerator();

                    constructorGen.Emit(OpCodes.Ldarg_0);
                    constructorGen.Emit(OpCodes.Newobj, listType.GetConstructor(new Type[] { typeof(int) }));
                    constructorGen.Emit(OpCodes.Castclass, listType);
                    constructorGen.Emit(OpCodes.Ret);

                    return (CreateListDelegate)getterMethod.CreateDelegate(typeof(CreateListDelegate));
                }
                #endregion

                #region Create setters
                private static object GetSetterElement(Type ownerType, FieldType fieldType, Type elementType)
                {
                    Type paramType = elementType.IsPrimitive || elementType == typeof(String) || elementType == typeof(DateTime) ?
                                         elementType :
                                        (elementType.IsEnum ? typeof(UInt32) : typeof(object));

                    Type generic = typeof(List<>).MakeGenericType(new Type[] { elementType });

                    DynamicMethod setterMethod = new DynamicMethod("", typeof(void),
                                                                   new Type[] { typeof(object), typeof(int), paramType },
                                                                   ownerType);
                    ILGenerator setterGen = setterMethod.GetILGenerator();

                    setterGen.Emit(OpCodes.Ldarg_0);
                    setterGen.Emit(OpCodes.Ldarg_2);
                    setterGen.Emit(OpCodes.Call, generic.GetMethod("Add"));
                    setterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Byte>));
                        case FieldType.SByte:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<SByte>));
                        case FieldType.Int16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<UInt64>));
                        case FieldType.Single:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Single>));
                        case FieldType.Double:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Double>));
                        case FieldType.DateTime:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<DateTime>));
                        case FieldType.String:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<String>));
                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                        case FieldType.Class:
                            return setterMethod.CreateDelegate(typeof(SetterDelegate<Object>));
                        default:
                            throw new NotSupportedException(String.Format("Can't create set delegate for List<{0}>, owner {1}.", elementType, ownerType));
                    }
                }
                #endregion

                #region Create getters
                private static object GetGetterElement(Type ownerType, FieldType fieldType, Type elementType)
                {
                    Type paramType = elementType.IsPrimitive || elementType == typeof(String) || elementType == typeof(DateTime) ?
                                         elementType :
                                        (elementType.IsEnum ? typeof(UInt32) : typeof(object));

                    Type generic = typeof(List<>).MakeGenericType(new Type[] { elementType });

                    DynamicMethod getterMethod = new DynamicMethod("", paramType,
                                                                   new Type[] { typeof(object), typeof(int) }, ownerType);
                    ILGenerator getterGen = getterMethod.GetILGenerator();

                    getterGen.Emit(OpCodes.Ldarg_0);
                    getterGen.Emit(OpCodes.Ldarg_1);
                    getterGen.Emit(OpCodes.Call, generic.GetProperty("Item").GetGetMethod());
                    getterGen.Emit(OpCodes.Ret);

                    switch (fieldType)
                    {
                        case FieldType.Boolean:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Boolean>));
                        case FieldType.Enum:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Byte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Byte>));
                        case FieldType.SByte:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<SByte>));
                        case FieldType.Int16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int16>));
                        case FieldType.UInt16:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt16>));
                        case FieldType.Int32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int32>));
                        case FieldType.UInt32:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt32>));
                        case FieldType.Int64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Int64>));
                        case FieldType.UInt64:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<UInt64>));
                        case FieldType.Single:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Single>));
                        case FieldType.Double:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Double>));
                        case FieldType.DateTime:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<DateTime>));
                        case FieldType.String:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<String>));
                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                        case FieldType.Class:
                            return getterMethod.CreateDelegate(typeof(GetterDelegate<Object>));
                        default:
                            throw new NotSupportedException(String.Format("Can't create get delegate for List<{0}>, owner {1}.", elementType, ownerType));
                    }
                }
                #endregion

                private readonly Type _genericType;

                private readonly IField _referenceField;

                private readonly object _setter;
                private readonly object _getter;

                private readonly CreateListDelegate _listConstructor;

                private readonly SetterDelegateReference _arraySetter;
                private readonly GetterDelegateReference _arrayGetter;

                public FieldList(byte id, Type ownerType, Type argumentType, bool writeDefault)
                    : base(id, ownerType, argumentType, writeDefault)
                {
                    CheckPrototypeDefined(argumentType);

                    _genericType = typeof(List<>).MakeGenericType(new Type[] { argumentType });

                    if (_fieldTypeId == FieldType.List)
                        _referenceField = new FieldList(0, ownerType, argumentType.GetGenericArguments()[0], true);
                    if (_fieldTypeId == FieldType.Array)
                        _referenceField = new FieldArray(0, ownerType, argumentType.GetElementType(), true);
                    if (_fieldTypeId == FieldType.Dictionary)
                        _referenceField = new FieldDictionary(0, ownerType, argumentType.GetGenericArguments()[0],
                                                                            argumentType.GetGenericArguments()[1],
                                                                            true);

                    _listConstructor = GetConstructorDelegate(ownerType, _genericType);

                    _setter = GetSetterElement(ownerType, _fieldTypeId, argumentType);
                    _getter = GetGetterElement(ownerType, _fieldTypeId, argumentType);
                }

                public FieldList(byte id, Type ownerType, FieldInfo info, bool writeDefault)
                    : this(id, ownerType, info.FieldType.GetGenericArguments()[0], writeDefault)
                {
                    _arraySetter = GetSetterReference(ownerType, info);
                    _arrayGetter = GetGetterReference(ownerType, info);
                }

                public override bool Serialize(object obj, NativeWriter writer)
                {
                    try
                    {
                        IList array = (IList)obj;
                        if (array == null)
                        {
                            if (_writeDefault)
                                writer.Write(-1);

                            return _writeDefault;
                        }

                        writer.Write(array.Count);
                        for (int i = 0, count = array.Count; i < count; i++)
                            SerializeElement(array, i, writer);

                        return true;
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s List<{1}> field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                public override bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        return Serialize(_arrayGetter(obj), writer);
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s List<{1}> field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                #region SerializeFrom array element
                private void SerializeElement(IList list, int index, NativeWriter writer)
                {
                    switch (_fieldTypeId)
                    {
                        case FieldType.Boolean:
                            writer.Write(((GetterDelegate<Boolean>)_getter)((List<Boolean>)list, index));
                            break;

                        case FieldType.Enum:
                            writer.Write(((GetterDelegate<UInt32>)_getter)((List<UInt32>)list, index));
                            break;

                        case FieldType.Byte:
                            writer.Write(((GetterDelegate<Byte>)_getter)((List<Byte>)list, index));
                            break;

                        case FieldType.SByte:
                            writer.Write(((GetterDelegate<SByte>)_getter)((List<SByte>)list, index));
                            break;

                        case FieldType.Int16:
                            writer.Write(((GetterDelegate<Int16>)_getter)((List<Int16>)list, index));
                            break;

                        case FieldType.UInt16:
                            writer.Write(((GetterDelegate<UInt16>)_getter)((List<UInt16>)list, index));
                            break;

                        case FieldType.Int32:
                            writer.Write(((GetterDelegate<Int32>)_getter)((List<Int32>)list, index));
                            break;

                        case FieldType.UInt32:
                            writer.Write(((GetterDelegate<UInt32>)_getter)((List<UInt32>)list, index));
                            break;

                        case FieldType.Int64:
                            writer.Write(((GetterDelegate<Int64>)_getter)((List<Int64>)list, index));
                            break;

                        case FieldType.UInt64:
                            writer.Write(((GetterDelegate<UInt64>)_getter)((List<UInt64>)list, index));
                            break;

                        case FieldType.Single:
                            writer.Write(((GetterDelegate<Single>)_getter)((List<Single>)list, index));
                            break;

                        case FieldType.Double:
                            writer.Write(((GetterDelegate<Double>)_getter)((List<Double>)list, index));
                            break;

                        case FieldType.DateTime:
                            writer.Write(((GetterDelegate<DateTime>)_getter)((List<DateTime>)list, index).Ticks);
                            break;

                        case FieldType.String:
                            writer.WriteUnicode(((GetterDelegate<String>)_getter)((List<String>)list, index));
                            break;

                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                            _referenceField.Serialize(list[index], writer);
                            break;

                        case FieldType.Class:
                            {
                                object fieldValue = list[index];

                                // тут постоянно в рантайме будем определять тип для поддержки полиморфизма
                                ushort fieldTypeKey = fieldValue != null ? _typeToKey[fieldValue.GetType()] : _nullTypeKey;

                                ISerializationPrototype prototype;
                                if (!_deserializationPrototypes.TryGetValue(fieldTypeKey, out prototype))
                                    throw new NotSupportedException(String.Format("Can't serialize {0} type.",
                                                                    fieldValue != null ? fieldValue.GetType().ToString() : "<null>"));

                                writer.Write(fieldTypeKey);
                                prototype.Serialize(fieldValue, writer);
                            }
                            break;

                        default:
                            throw new NotSupportedException(String.Format("Can't serialize {0} field.", _fieldTypeId));
                    }
                }
                #endregion

                public override void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        _arraySetter(obj, Deserialize(reader));
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s List<{1}> field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                public override object Deserialize(NativeReader reader)
                {
                    try
                    {
                        int length = reader.ReadInt32();

                        IList array = length > -1 ? _listConstructor(length) : null;
                        for (int i = 0; i < length; i++)
                            DeserializeElement(array, i, reader);

                        return array;
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s List<{1}> field.", _ownerType, _fieldType);
                        throw;
                    }
                }

                #region DeserializeTo array element
                private void DeserializeElement(IList list, int index, NativeReader reader)
                {
                    switch (_fieldTypeId)
                    {
                        case FieldType.Boolean:
                            ((SetterDelegate<Boolean>)_setter)((List<Boolean>)list, index, reader.ReadBoolean());
                            break;

                        case FieldType.Enum:
                            ((SetterDelegate<UInt32>)_setter)((List<UInt32>)list, index, reader.ReadUInt32());
                            break;

                        case FieldType.Byte:
                            ((SetterDelegate<Byte>)_setter)((List<Byte>)list, index, reader.ReadByte());
                            break;

                        case FieldType.SByte:
                            ((SetterDelegate<SByte>)_setter)((List<SByte>)list, index, reader.ReadSByte());
                            break;

                        case FieldType.Int16:
                            ((SetterDelegate<Int16>)_setter)((List<Int16>)list, index, reader.ReadInt16());
                            break;

                        case FieldType.UInt16:
                            ((SetterDelegate<UInt16>)_setter)((List<UInt16>)list, index, reader.ReadUInt16());
                            break;

                        case FieldType.Int32:
                            ((SetterDelegate<Int32>)_setter)((List<Int32>)list, index, reader.ReadInt32());
                            break;

                        case FieldType.UInt32:
                            ((SetterDelegate<UInt32>)_setter)((List<UInt32>)list, index, reader.ReadUInt32());
                            break;

                        case FieldType.Int64:
                            ((SetterDelegate<Int64>)_setter)((List<Int64>)list, index, reader.ReadInt64());
                            break;

                        case FieldType.UInt64:
                            ((SetterDelegate<UInt64>)_setter)((List<UInt64>)list, index, reader.ReadUInt64());
                            break;

                        case FieldType.Single:
                            ((SetterDelegate<Single>)_setter)((List<Single>)list, index, reader.ReadSingle());
                            break;

                        case FieldType.Double:
                            ((SetterDelegate<Double>)_setter)((List<Double>)list, index, reader.ReadDouble());
                            break;

                        case FieldType.DateTime:
                            ((SetterDelegate<DateTime>)_setter)((List<DateTime>)list, index, new DateTime(reader.ReadInt64()));
                            break;

                        case FieldType.String:
                            ((SetterDelegate<String>)_setter)((List<String>)list, index, reader.ReadUnicode());
                            break;

                        case FieldType.Array:
                        case FieldType.List:
                        case FieldType.Dictionary:
                            list.Add(_referenceField.Deserialize(reader));
                            break;

                        case FieldType.Class:
                            {
                                ushort typeKey = reader.ReadUInt16();

                                ISerializationPrototype prototype;
                                if (!_deserializationPrototypes.TryGetValue(typeKey, out prototype))
                                    throw new NotSupportedException(String.Format("Can't deserialize for {0} type key.", typeKey));

                                list.Add(prototype.Deserialize(reader));
                            }
                            break;

                        default:
                            throw new NotSupportedException(String.Format("Can't deserialize {0} field.", _fieldTypeId));
                    }
                }
                #endregion
            }

            private class FieldDictionary : IField
            {
                #region Constructor delegate
                protected delegate object CreateDictionaryElementDelegate();
                protected delegate IKeyValueCollection CreateDictionaryDelegate(int capacity);
                #endregion

                #region Create constructor
                private static CreateDictionaryElementDelegate GetDictionaryElementConstructorDelegate(Type ownerType, Type elementType)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", elementType, new Type[] { }, ownerType);
                    ILGenerator constructorGen = getterMethod.GetILGenerator();

                    constructorGen.Emit(OpCodes.Newobj, elementType.GetConstructor(new Type[] { }));
                    constructorGen.Emit(OpCodes.Castclass, elementType);
                    constructorGen.Emit(OpCodes.Ret);

                    return (CreateDictionaryElementDelegate)getterMethod.CreateDelegate(typeof(CreateDictionaryElementDelegate));
                }

                private static CreateDictionaryDelegate GetDictionaryConstructorDelegate(Type ownerType, Type dictionaryType)
                {
                    DynamicMethod getterMethod = new DynamicMethod("", dictionaryType, new Type[] { typeof(int) }, ownerType);
                    ILGenerator constructorGen = getterMethod.GetILGenerator();

                    constructorGen.Emit(OpCodes.Ldarg_0);
                    constructorGen.Emit(OpCodes.Newobj, dictionaryType.GetConstructor(new Type[] { typeof(int) }));
                    constructorGen.Emit(OpCodes.Castclass, dictionaryType);
                    constructorGen.Emit(OpCodes.Ret);

                    return (CreateDictionaryDelegate)getterMethod.CreateDelegate(typeof(CreateDictionaryDelegate));
                }
                #endregion

                private readonly byte _id;

                private readonly bool _writeDefault = false;

                protected readonly Type _ownerType;

                protected readonly Type _keyType;
                protected readonly uint _keyTypeKey;
                protected readonly Type _valueType;
                protected readonly uint _valueTypeKey;

                protected readonly Field.FieldType _keyTypeId = Field.FieldType.Class;
                protected readonly Field.FieldType _valueTypeId = Field.FieldType.Class;

                private readonly Type _genericType;
                private readonly Type _genericTypeElement;

                private readonly CreateDictionaryElementDelegate _dictionaryElementConstructor;
                private readonly CreateDictionaryDelegate _dictionaryConstructor;

                private readonly SetterDelegateReference _setter;
                private readonly GetterDelegateReference _getter;

                private readonly IField _keyField;
                private readonly IField _valueField;

                public FieldDictionary(byte id, Type ownerType, Type keyType, Type valueType, bool writeDefault)
                {
                    CheckPrototypeDefined(keyType);
                    CheckPrototypeDefined(valueType);

                    _genericType = typeof(KeyValueCollection<,>).MakeGenericType(new Type[] { keyType, valueType });
                    _genericTypeElement = typeof(KeyValue<,>).MakeGenericType(new Type[] { keyType, valueType });

                    _id = id;

                    _writeDefault = writeDefault;

                    _ownerType = ownerType;

                    _keyType = keyType;
                    _valueType = valueType;
                    _keyTypeKey = _typeToKey[keyType];
                    _valueTypeKey = _typeToKey[valueType];

                    _keyTypeId = Field.GetFieldType(keyType);
                    _valueTypeId = Field.GetFieldType(valueType);

                    _dictionaryElementConstructor = GetDictionaryElementConstructorDelegate(ownerType, _genericTypeElement);
                    _dictionaryConstructor = GetDictionaryConstructorDelegate(ownerType, _genericType);

                    _keyField = CreateFieldDescription(0, _genericTypeElement, _genericTypeElement.GetField("Key"), true);
                    _valueField = CreateFieldDescription(0, _genericTypeElement, _genericTypeElement.GetField("Value"), true);
                }

                public FieldDictionary(byte id, Type ownerType, FieldInfo info, bool writeDefault)
                    : this(id, ownerType, info.FieldType.GetGenericArguments()[0], info.FieldType.GetGenericArguments()[1], writeDefault)
                {
                    _setter = GetSetterReference(ownerType, info);
                    _getter = GetGetterReference(ownerType, info);
                }

                public bool Serialize(object obj, NativeWriter writer)
                {
                    try
                    {
                        IKeyValueCollection dictionary = (IKeyValueCollection)obj;
                        if (dictionary == null)
                        {
                            if (_writeDefault)
                                writer.Write(-1);

                            return _writeDefault;
                        }

                        writer.Write(dictionary.Count);
                        for (int i = 0, count = dictionary.Count; i < count; i++)
                        {
                            object element = dictionary.GetByIndex(i);

                            _keyField.SerializeFrom(element, writer);
                            _valueField.SerializeFrom(element, writer);
                        }

                        return true;
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s KeyValueCollection<{1}, {2}> field.",
                                                         _ownerType, _keyType, _valueType);
                        throw;
                    }
                }

                public bool SerializeFrom(object obj, NativeWriter writer)
                {
                    try
                    {
                        return Serialize(_getter(obj), writer);
                    }
                    catch
                    {
                        Logger.Error("Can't serialize {0}'s KeyValueCollection<{1}, {2}> field.",
                                                         _ownerType, _keyType, _valueType);
                        throw;
                    }
                }

                public void DeserializeTo(object obj, NativeReader reader)
                {
                    try
                    {
                        _setter(obj, Deserialize(reader));
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s KeyValueCollection<{1}, {2}> field.",
                                                         _ownerType, _keyType, _valueType);
                        throw;
                    }
                }

                public object Deserialize(NativeReader reader)
                {
                    try
                    {
                        int length = reader.ReadInt32();

                        IKeyValueCollection dictionary = length > -1 ? _dictionaryConstructor(length) : null;
                        if (dictionary != null)
                        {
                            for (int i = 0; i < length; i++)
                            {
                                object element = _dictionaryElementConstructor();

                                _keyField.DeserializeTo(element, reader);
                                _valueField.DeserializeTo(element, reader);

                                dictionary.Add(element);
                            }

                            dictionary.OnDeserializedMethod();
                        }

                        return dictionary;
                    }
                    catch
                    {
                        Logger.Error("Can't deserialize {0}'s KeyValueCollection<{1}, {2}> field.",
                                                         _ownerType, _keyType, _valueType);
                        throw;
                    }
                }

                public byte Id
                {
                    get { return _id; }
                }
            }
            #endregion

            protected readonly Type _type;
            protected readonly ushort _typeKey;

            private byte _fieldId;

            private readonly List<IField> _fields = new List<IField>();
            private readonly Dictionary<byte, IField> _fieldsByKey = new Dictionary<byte, IField>();

            public SerializationPrototype(Type type, ushort typeKey)
            {
                if (IsStruct(type))
                    throw new NotSupportedException(String.Format("Field<Type:{0}> serialization isn't supported.", type));

                _type = type;
                _typeKey = typeKey;

                // разобрать тип
                BuildFieldDescriptions(type);

                Type baseType = type.BaseType;
                while (baseType != typeof(object))
                {
                    BuildFieldDescriptions(baseType);
                    baseType = baseType.BaseType;
                }
            }

            private void BuildFieldDescriptions(Type type)
            {
                foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                               BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!fieldInfo.ContainsAttribute<DataMemberAttribute>())
                        continue;

                    IField field = CreateFieldDescription(GetNextFieldId(), type, fieldInfo, false);

                    _fields.Add(field);
                    _fieldsByKey.Add(field.Id, field);
                }
            }

            private byte GetNextFieldId()
            {
                if (_fieldId == byte.MaxValue)
                    throw new IndexOutOfRangeException("Data type for field serialization key is to small.");

                return _fieldId++;
            }

            private static IField CreateFieldDescription(byte fieldId, Type ownerType, FieldInfo fieldInfo, bool writeDefault)
            {
                // NOTE : без супер мега необходимости не менять порядок этих иф-ов
                Type fieldType = fieldInfo.FieldType;
                if (fieldType.IsArray)
                    return new FieldArray(fieldId, ownerType, fieldInfo, writeDefault);

                if (fieldType.IsGenericType)
                {
                    if (fieldType.GetGenericTypeDefinition().Equals(typeof(List<>)))
                        return new FieldList(fieldId, ownerType, fieldInfo, writeDefault);

                    if (fieldType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                        return new FieldNullable(fieldId, ownerType, fieldInfo);

                    if (fieldType.GetGenericTypeDefinition().Equals(typeof(KeyValueCollection<,>)))
                        return new FieldDictionary(fieldId, ownerType, fieldInfo, writeDefault);
                }

                if (fieldType.Equals(typeof(DateTime)))
                    return new FieldDateTime(fieldId, ownerType, fieldInfo);

                if (fieldType.Equals(typeof(Object)))
                    throw new NotSupportedException("Try to use another type of fields neither object.");

                if (fieldType.IsValueType || (fieldType.IsClass && !fieldType.IsGenericType))
                    //|| typeof(RelationBase).IsAssignableFrom(fieldType) 
                    //|| fieldType.GetGenericTypeDefinition().Equals(typeof(DataChunkDifference<,>))
                    //|| fieldType.GetGenericTypeDefinition().Equals(typeof(DataChunk<>)))
                    return new Field(fieldId, ownerType, fieldInfo, writeDefault);

                throw new NotSupportedException(String.Format("Field<Type:{0}> serialization isn't supported.", fieldType));
            }

            private static bool IsStruct(Type type)
            {
                return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
            }

            public void Serialize(object obj, NativeWriter writer)
            {
                try
                {
                    long lenPos = writer.Position;
                    writer.Position += sizeof(long);

                    for (int i = 0, count = _fields.Count; i < count; i++)
                    {
                        long idPos = writer.Position;
                        writer.Position += sizeof(byte);

                        if (_fields[i].SerializeFrom(obj, writer))
                        {
                            long oldPos = writer.Position;

                            writer.Position = idPos;
                            writer.Write(_fields[i].Id);

                            writer.Position = oldPos;
                        }
                        else
                            writer.Position -= sizeof(byte);
                    }

                    long endPos = writer.Position;

                    writer.Position = lenPos;
                    writer.Write(endPos - lenPos - sizeof(long));

                    writer.Position = endPos;
                }
                catch
                {
                    Logger.Error("Can't serialize {0} type.", _type);
                    throw;
                }
            }

            public object Deserialize(NativeReader reader)
            {
                try
                {
                    object obj = FormatterServices.GetUninitializedObject(_type);

                    long endPos = reader.ReadInt64() + reader.Position;
                    while (reader.Position != endPos)
                    {
                        byte fieldId = reader.ReadByte();

                        IField field;
                        if (!_fieldsByKey.TryGetValue(fieldId, out field))
                            throw new IndexOutOfRangeException(String.Format("Can't deserialize Field<Id:{0}> of {1}.",
                                                                             fieldId, _type));

                        field.DeserializeTo(obj, reader);

                        if (reader.Position > endPos)
                            throw new IndexOutOfRangeException("Incorrect deserialization data.");
                    }

                    return obj;
                }
                catch
                {
                    Logger.Error("Can't deserialize {0} type.", _type);
                    throw;
                }
            }

            public Type Type
            {
                get { return _type; }
            }

            public ushort TypeKey
            {
                get { return _typeKey; }
            }
        }
        #endregion

        private const ushort _nullTypeKey = 0;
        private static ushort _typeKey = _nullTypeKey + 1;

        private readonly static Dictionary<Type, ushort> _typeToKey = new Dictionary<Type, ushort>();
        private readonly static Dictionary<ushort, Type> _keyToType = new Dictionary<ushort, Type>();

        private readonly static Dictionary<ushort, ISerializationPrototype> _deserializationPrototypes =
            new Dictionary<ushort, ISerializationPrototype>();

        public readonly static Logger Logger = LogManager.GetLogger("Serialization");

        static FastSerializer()
        {
            CreatePrototypeNull();

            RegisterTypeAndKey(typeof(Object), GetNextTypeKey());

            RegisterTypeAndKey(typeof(Boolean), GetNextTypeKey());

            RegisterTypeAndKey(typeof(Byte), GetNextTypeKey());
            RegisterTypeAndKey(typeof(SByte), GetNextTypeKey());
            RegisterTypeAndKey(typeof(Int16), GetNextTypeKey());
            RegisterTypeAndKey(typeof(UInt16), GetNextTypeKey());
            RegisterTypeAndKey(typeof(Int32), GetNextTypeKey());
            RegisterTypeAndKey(typeof(UInt32), GetNextTypeKey());
            RegisterTypeAndKey(typeof(Int64), GetNextTypeKey());
            RegisterTypeAndKey(typeof(UInt64), GetNextTypeKey());

            RegisterTypeAndKey(typeof(Single), GetNextTypeKey());
            RegisterTypeAndKey(typeof(Double), GetNextTypeKey());

            RegisterTypeAndKey(typeof(String), GetNextTypeKey());
            RegisterTypeAndKey(typeof(DateTime), GetNextTypeKey());

            CreatePrototypeArrayByte(GetNextTypeKey());


            // create prototypes of all class with DataContract attribute
            foreach (Type type in Assembly.GetCallingAssembly().GetTypes())
                if (!type.IsAbstract && !type.IsInterface && !type.IsGenericType)
                    if (type.ContainsAttribute<DataContractAttribute>())
                        if (!_typeToKey.ContainsKey(type))
                            CreatePrototype(type, GetNextTypeKey());
        }

        public static ushort GetTypeKey(Type type)
        {
            return _typeToKey[type];
        }

        private static ushort GetNextTypeKey()
        {
            if (_typeKey == ushort.MaxValue)
                throw new IndexOutOfRangeException("Data type for type serialization key is to small.");

            return _typeKey++;
        }

        private static void CreatePrototype(Type type, ushort key)
        {
            try
            {
                RegisterTypeAndKey(type, key);

                RegisterPrototype(new SerializationPrototype(type, key));
            }
            catch
            {
                Logger.Error("Can't create prototype for {0}.", type);
                throw;
            }
        }

        private static void CreatePrototypeNull()
        {
            try
            {
                _keyToType.Add(_nullTypeKey, null);

                RegisterPrototype(new SerializationPrototypeNull(_nullTypeKey));
            }
            catch
            {
                Logger.Error("Can't create prototype for nulltype.");
                throw;
            }
        }

        private static void CreatePrototypeArrayByte(ushort key)
        {
            try
            {
                RegisterTypeAndKey(typeof(Byte[]), key);

                RegisterPrototype(new SerializationPrototypeArrayByte(key));
            }
            catch
            {
                Logger.Error("Can't create prototype for byte array.");
                throw;
            }
        }

        private static void RegisterTypeAndKey(Type type, ushort key)
        {
            _typeToKey.Add(type, key);
            _keyToType.Add(key, type);
        }

        private static void RegisterPrototype(ISerializationPrototype prototype)
        {
            _deserializationPrototypes.Add(prototype.TypeKey, prototype);
        }

        private static void CheckPrototypeDefined(Type prototype)
        {
            if (_typeToKey.ContainsKey(prototype))
                return;

            if (prototype.IsPrimitive || prototype == typeof(String))
                throw new NotSupportedException(String.Format("Primitive type {0} doesn't supported by FastSerializer.", prototype));

            if (prototype.IsEnum || prototype.IsArray ||
                (prototype.IsGenericType && prototype.GetGenericTypeDefinition().Equals(typeof(List<>))))
            {
                RegisterTypeAndKey(prototype, GetNextTypeKey());
                return;
            }

            if (!prototype.ContainsAttribute<DataContractAttribute>())
                throw new NotSupportedException(String.Format("Type {0} doesn't marked by [DataContract] attribute.", prototype));

            CreatePrototype(prototype, GetNextTypeKey());
            return;
        }

        public void Serialize(object obj, NativeWriter writer)
        {
            ushort objectTypeKey = obj != null ? _typeToKey[obj.GetType()] : _nullTypeKey;

            try
            {
                ISerializationPrototype prototype;
                if (!_deserializationPrototypes.TryGetValue(objectTypeKey, out prototype))
                    throw new NotSupportedException(String.Format("Can't serialize object of type {0}",
                                                                  obj != null ? obj.GetType().ToString() : "<null>"));

                writer.Write(prototype.TypeKey);
                prototype.Serialize(obj, writer);
            }
            catch
            {
                Logger.Error("Can't serialize object of type {0}",
                                                 obj != null ? obj.GetType().ToString() : "<null>");
                throw;
            }
        }

        public object Deserialize(NativeReader reader)
        {
            ushort typeKey = reader.ReadUInt16();

            try
            {
                ISerializationPrototype prototype;
                if (!_deserializationPrototypes.TryGetValue(typeKey, out prototype))
                    throw new NotSupportedException(String.Format("Can't deserialize object for key {0}", typeKey));

                return prototype.Deserialize(reader);
            }
            catch
            {
                Logger.Error("Can't deserialize object for key {0}", typeKey);
                throw;
            }
        }

        public Type ReadType(NativeReader reader)
        {
            ushort typeKey = reader.ReadUInt16();

            Type type;
            if (!_keyToType.TryGetValue(typeKey, out type))
                throw new NotSupportedException(String.Format("Can't find type for Key<Id:{0}>", typeKey));

            return type;
        }
    }
}
