using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EchoDigWinServer.Packets;

namespace EchoDigWinServer
{
    public enum PacketType : byte
    {
        Login = 0x01,
        Audio = 0x02
    }

    public class Packet
    {
        public byte[] Buffer = new byte[1105920];
        public int Position;
        public int BufferLength;

        public Packet()
        {
            Position = 0;
            BufferLength = 0;
        }

        public PacketType GetPacketType()
        {
            return (PacketType)Buffer[Position++];
        }

        public bool Parse()
        {
            PacketType type = GetPacketType();
            switch (type)
            {
                case PacketType.Login:
                    LoginPacket lPacket = LoginPacket.Parse(this);
                    if (!lPacket.Nick.ToUpper().Equals("CAIOKETO"))
                    {
                        return false;
                    }
                    if (!lPacket.Password.ToUpper().Equals("123"))
                    {
                        return false;
                    }
                    break;
                case PacketType.Audio:
                    break;
                default:
                    break;
            }
            return true;
        }

        public byte GetByte()
        {
            return Buffer[Position++];
        }

        public byte[] GetBytes(int len)
        {
            byte[] result = new byte[len];
            Array.Copy(Buffer, Position, result, 0, len);
            Position += len;
            return result;
        }

        public uint GetUInt32()
        {
            return BitConverter.ToUInt32(GetBytes(4), 0);
        }

        public ushort GetUInt16()
        {
            return BitConverter.ToUInt16(GetBytes(2), 0);
        }

        public string GetString()
        {
            int strLen = (int)GetUInt16();
            string t = System.Text.ASCIIEncoding.Default.GetString(Buffer, Position, strLen);
            Position += strLen;
            return t;
        }

        public void AddBytes(byte[] value)
        {
            Array.Copy(value, 0, Buffer, Position, value.Length);
            Position += value.Length;

            if (Position > BufferLength)
                BufferLength = Position;
        }

        public void AddByte(byte value)
        {
            AddBytes(new byte[] { value });
        }

        public void AddUInt32(uint value)
        {
            AddBytes(BitConverter.GetBytes(value));
        }

        public void AddLength()
        {
            byte[] value = BitConverter.GetBytes((uint)BufferLength);
            Array.Copy(value, 0, Buffer, 1, value.Length);
        }

        public void PrepareToSend()
        {
            Array.Copy(Buffer, 1, Buffer, 5, BufferLength);
            AddLength();
        }
    }
}
