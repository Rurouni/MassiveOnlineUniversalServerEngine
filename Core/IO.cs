using System;
using System.IO;
using System.Text;

namespace MOUSE.Core
{
    public class NativeWriter : Stream
    {
        private long _pos;
        private long _rightBorder;
        private Byte[] _buff;
        private readonly bool _isStretchable;
        private Byte _stretchFactor;

        private void RaiseExceptionIfCantWrite(int sizeInBytes)
        {
            if (_pos + sizeInBytes > _buff.Length)
            {
                if (!_isStretchable)
                    throw new Exception("Can't write to buff cos it is full");

                if (_buff.Length >= 1024 * 1024 * 1024)
                    throw new Exception(string.Format("Can't write to buff cos its full and size is too big {0}", _buff.Length));

                ++_stretchFactor;

                Array.Resize(ref _buff, _buff.Length * 10);
                SetBuffer(_buff);

                RaiseExceptionIfCantWrite(sizeInBytes);
            }
        }

        public NativeWriter(Byte[] buff)
            : this(buff, false)
        {
        }

        public NativeWriter()
            : this(new byte[1024], true)
        {
        }

        public NativeWriter(Byte[] buff, bool isStretchable)
        {
            _pos = 0;
            SetBuffer(buff);
            _isStretchable = isStretchable;
            _stretchFactor = 0;
        }

        public void SetBuffer(Byte[] buff)
        {
            _buff = buff;
            _rightBorder = _buff.Length;
        }

        public override void SetLength(long value)
        {
            _rightBorder = value;
        }

        public Byte[] Buff
        {
            get { return _buff; }
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return _rightBorder; }
        }

        public override long Position
        {
            get { return _pos; }
            set { _pos = value; }
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _pos = offset;
                    break;
                case SeekOrigin.Current:
                    _pos += offset;
                    break;
                case SeekOrigin.End:
                    _pos = _rightBorder - offset - 1;
                    break;
            }

            return _pos;
        }

        public override int Read(byte[] buff, int offset, int count)
        {
            return 0;
        }

        #region Write methods

        private unsafe void WriteToBuffer(byte* value, int size)
        {
            RaiseExceptionIfCantWrite(size);

            fixed (byte* bptsDestination = &_buff[_pos])
                for (int i = 0; i < size; i++)
                    bptsDestination[i] = value[i];

            _pos += size;
        }

        public void Write(byte value)
        {
            RaiseExceptionIfCantWrite(1);
            _buff[_pos++] = value;
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public unsafe void Write(byte[] buffer, int length)
        {
            if (buffer.Length > 0)
                fixed (byte* value = &buffer[0])
                    WriteToBuffer(value, length);
        }

        public unsafe override void Write(byte[] buffer, int index, int count)
        {
            if (buffer.Length > 0)
                fixed (byte* value = &buffer[index])
                    WriteToBuffer(value, count);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, buffer.Length);
        }

        public unsafe void Write(double value)
        {
            WriteToBuffer((byte*)&value, sizeof(double));
        }

        public unsafe void Write(float value)
        {
            WriteToBuffer((byte*)&value, sizeof(float));
        }

        public unsafe void Write(int value)
        {
            WriteToBuffer((byte*)&value, sizeof(int));
        }

        public unsafe void Write(uint value)
        {
            WriteToBuffer((byte*)&value, sizeof(uint));
        }

        public unsafe void Write(long value)
        {
            WriteToBuffer((byte*)&value, sizeof(long));
        }

        public unsafe void Write(ulong value)
        {
            WriteToBuffer((byte*)&value, sizeof(ulong));
        }

        public void Write(sbyte value)
        {
            RaiseExceptionIfCantWrite(1);
            _buff[_pos++] = (byte)(value);
        }

        public unsafe void Write(short value)
        {
            WriteToBuffer((byte*)&value, sizeof(short));
        }

        public unsafe void Write(ushort value)
        {
            WriteToBuffer((byte*)&value, sizeof(ushort));
        }

        public void WriteUnicode(string value)
        {
            if (value != null)
            {
                Write(value.Length);
                RaiseExceptionIfCantWrite(value.Length * 2);
                _pos += Encoding.Unicode.GetBytes(value, 0, value.Length, _buff, (int)_pos);
            }
            else
                Write(-1);
        }

        public void WriteASCII(string value)
        {
            if (value != null)
            {
                Write(value.Length);
                RaiseExceptionIfCantWrite(value.Length);
                _pos += Encoding.ASCII.GetBytes(value, 0, value.Length, _buff, (int)_pos);
            }
            else
                Write(-1);
        }



        #endregion
    }

    public class NativeReader : Stream
    {
        private int _pos;
        private int _rightBorder;
        private byte[] _buff;

        public void SetBuffer(byte[] buffer, int position, int count)
        {
            _buff = buffer;
            _pos = position;
            _rightBorder = position + count;
        }

        public void SetBuffer(byte[] buffer, int position)
        {
            SetBuffer(buffer, position, buffer.Length - position);
        }

        public Byte[] GetBuffer()
        {
            return _buff;
        }

        public void SetPosition(int position)
        {
            _pos = position;
        }

        private void RaiseExceptionIfCantRead(int sizeInBytes)
        {
            if (_pos + sizeInBytes > _rightBorder)
                throw new Exception("Can't read from buff.");
        }
        #region Stream stuff

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return _pos = (int)offset;
                case SeekOrigin.Current:
                    return _pos += (int)offset;
                case SeekOrigin.End:
                    return _pos = _rightBorder - (int)offset;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }
        }

        public override void SetLength(long value)
        {
            _rightBorder = (int)value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            RaiseExceptionIfCantRead(count);
            int bytesLeft = _rightBorder - _pos;
            int realCount = bytesLeft < count ? bytesLeft : count;
            Array.Copy(_buff, _pos, buffer, offset, realCount);
            _pos += realCount;

            return realCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // DO NOTHING we are read only stream
        }

        public override void WriteByte(byte value)
        {
            // DO NOTHING we are read only stream
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _rightBorder; }
        }

        public override long Position
        {
            get { return _pos; }
            set { _pos = (int)value; }
        }
        #endregion

        #region Read methods

        public bool ReadBoolean()
        {
            RaiseExceptionIfCantRead(sizeof(bool));
            return _buff[_pos++] > 0;
        }

        public new byte ReadByte()
        {
            RaiseExceptionIfCantRead(sizeof(byte));
            return _buff[_pos++];
        }

        public byte[] ReadAll()
        {
            byte[] bts = new byte[_rightBorder - _pos];
            Read(bts, 0, bts.Length);
            return bts;
        }

        public unsafe double ReadDouble()
        {
            RaiseExceptionIfCantRead(sizeof(double));
            double value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 8; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 8;
            return value;
        }

        public unsafe float ReadSingle()
        {
            RaiseExceptionIfCantRead(sizeof(float));
            float value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 4; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 4;
            return value;
        }

        public unsafe int ReadInt32()
        {
            RaiseExceptionIfCantRead(sizeof(int));
            int value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 4; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 4;
            return value;
        }

        public unsafe long ReadInt64()
        {
            RaiseExceptionIfCantRead(sizeof(long));
            long value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 8; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 8;
            return value;
        }

        public sbyte ReadSByte()
        {
            RaiseExceptionIfCantRead(sizeof(byte));
            return (sbyte)_buff[_pos++];
        }

        public unsafe short ReadInt16()
        {
            RaiseExceptionIfCantRead(sizeof(short));
            short value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 2; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 2;
            return value;
        }

        public string ReadUnicode()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;

            RaiseExceptionIfCantRead(2 * len);
            string str = Encoding.Unicode.GetString(_buff, _pos, 2 * len);
            _pos += 2 * len;
            return str;
        }

        public string ReadASCII()
        {
            int len = ReadInt32();
            if (len == -1)
                return null;

            RaiseExceptionIfCantRead(len);
            string str = Encoding.ASCII.GetString(_buff, _pos, len);
            _pos += len;
            return str;
        }

        public unsafe uint ReadUInt32()
        {
            RaiseExceptionIfCantRead(sizeof(uint));
            uint value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 4; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 4;
            return value;
        }

        public unsafe ulong ReadUInt64()
        {
            RaiseExceptionIfCantRead(sizeof(ulong));
            ulong value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 8; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 8;
            return value;
        }

        public unsafe ushort ReadUInt16()
        {
            RaiseExceptionIfCantRead(sizeof(ushort));
            ushort value;
            byte* bptsDestination = (byte*)&value;
            fixed (byte* bptsSource = &_buff[_pos])
                for (int i = 0; i < 2; i++)
                    bptsDestination[i] = bptsSource[i];
            _pos += 2;
            return value;
        }

        #endregion
    }
}
