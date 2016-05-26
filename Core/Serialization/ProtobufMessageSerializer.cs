using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core.Interfaces.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;

namespace MOUSE.Core.Serialization
{
    public class ProtobufMessageSerializer : IMessageSerializer
    {
        readonly RuntimeTypeModel _model;
        readonly Dictionary<int, Type> _messageIdToType = new Dictionary<int, Type>();

        public ProtobufMessageSerializer(IEnumerable<Message> messages, IEnumerable<MessageHeader> headers)
        {
            _model = TypeModel.Create();
            _model.AllowParseableTypes = true;
            _model[typeof(Message)].UseConstructor = false;

            foreach (var message in messages)
            {
                if (message.GetType() == typeof(Message))
                    continue;

                var type = message.GetType();
                _model[typeof(Message)].AddSubType(GenerateId(type), type);
                _model[type].UseConstructor = false;
            }


            _model[typeof(MessageHeader)].UseConstructor = false;

            foreach (var header in headers)
            {
                if (header.GetType() == typeof(MessageHeader))
                    continue;

                var type = header.GetType();
                _model[typeof(MessageHeader)].AddSubType(GenerateId(type), type);
                _model[type].UseConstructor = false;
            }

            _model.CompileInPlace();
        }

        public ProtobufMessageSerializer(params Assembly[] protocolAssemblies)
        {
            var types = protocolAssemblies.SelectMany(assembly => assembly.GetTypes());
            _model = TypeModel.Create();
            _model.AllowParseableTypes = true;
            _model[typeof(Message)].UseConstructor = false;

            foreach (var type in types.Where(t => typeof(Message).IsAssignableFrom(t) && t != typeof(Message)))
            {
                _model[typeof(Message)].AddSubType(GenerateId(type), type);
                _model[type].UseConstructor = false;
            }


            _model[typeof(MessageHeader)].UseConstructor = false;

            foreach (var type in types.Where(t => typeof(MessageHeader).IsAssignableFrom(t) && t != typeof(MessageHeader)))
            {
                _model[typeof(MessageHeader)].AddSubType(GenerateId(type), type);
                _model[type].UseConstructor = false;
            }

            _model.CompileInPlace();
        }

        int GenerateId(Type type)
        {
            int id = GetHashCode(type.Name);

            if (_messageIdToType.ContainsKey(id))
                throw new Exception("Hash collision occured for " + type.Name);

            _messageIdToType.Add(id, type);

            return id;
        }

        static int GetHashCode(string str)
        {
            return (int)(Crc32.Compute(Encoding.ASCII.GetBytes(str)) % 100000000); //ugly hack for protobuf;
        }

        public bool TryReadType(ArraySegment<byte> data, out Type type)
        {
            using (var stream = new MemoryStream(data.Array, data.Offset, data.Count))
            {
                var reader = new ProtoReader(stream, null, null);
                int typeId = reader.ReadFieldHeader();
                return _messageIdToType.TryGetValue(typeId, out type);
            }
        }

        public void Serialize(Message msg, Stream stream)
        {
            _model.Serialize(stream, msg);
        }

        public bool TryDeserialize(ArraySegment<byte> data, out Message msg)
        {
            try
            {
                using (var stream = new MemoryStream(data.Array, data.Offset, data.Count))
                {
                    msg = (Message)_model.Deserialize(stream, null, typeof(Message));
                }
                return true;
            }
            catch (Exception)
            {
                msg = null;
                return false;
            }
        }

        public string GetProtoManifest()
        {
            return _model.GetSchema(null);
        }

        public string MimeType
        {
            get { return "application/x-protobuf"; }
        }
    }
}
