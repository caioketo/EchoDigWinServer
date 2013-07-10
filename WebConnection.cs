using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace EchoDigWinServer
{
    public class WebConnection
    {
        protected Socket socket;
        protected NetworkStream stream;
        protected byte[] Buffer = new byte[1105920];
        protected Packet inPacket = new Packet();

        public WebConnection()
        {
        }


        new public void ListenerCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            socket = listener.EndAccept(ar);
            socket.BeginReceive(Buffer, 0, 1024, 0, new AsyncCallback(DoShake), null);

            stream = new NetworkStream(socket);
        }

        public String ComputeWebSocketHandshakeSecurityHash09(String secWebSocketKey)
        {
            const String MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            String secWebSocketAccept = String.Empty;

            String ret = secWebSocketKey + MagicKEY;

            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha.ComputeHash(Encoding.ASCII.GetBytes(ret));

            secWebSocketAccept = Convert.ToBase64String(sha1Hash);

            return secWebSocketAccept;
        }

        private void DoShake(IAsyncResult ar)
        {
            int recievedByteCount = socket.EndReceive(ar);

            var utf8_handshake = Encoding.UTF8.GetString(Buffer, 0, 1024);
            string[] handshakeText = utf8_handshake.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string key = "";
            string accept = "";
            foreach (string line in handshakeText)
            {
                if (line.Contains("Sec-WebSocket-Key:"))
                {
                    key = line.Substring(line.IndexOf(":") + 2);
                }
            }

            if (key != "")
            {
                accept = ComputeWebSocketHandshakeSecurityHash09(key);
            }

            var stringShake = "HTTP/1.1 101 Web Socket Protocol Handshake\r\n" +
                                "Upgrade: WebSocket\r\n" +
                                "Connection: Upgrade\r\n";
            stringShake += "Sec-WebSocket-Accept: " + accept + "\r\n" + "\r\n";

            byte[] response = Encoding.UTF8.GetBytes(stringShake);

            socket.Send(response);

            socket.BeginReceive(Buffer, 0, 1105920, 0, new AsyncCallback(ClientReadCallback), null);
        }

        new public void ClientReadCallback(IAsyncResult ar)
        {
            if (!EndRead(ar))
            {
                return;
            }

            if (inPacket.Parse())
            {
                socket.BeginReceive(Buffer, 0, 1105920, 0, new AsyncCallback(ClientReadCallback), null);
            }
        }

        private bool EndRead(IAsyncResult ar)
        {
            try
            {
                int read = socket.EndReceive(ar);

                if (read == 0)
                {
                    return false;
                }

                byte firstByte = Buffer[0];
                byte secondByte = Buffer[1];

                if (firstByte != 0x81)
                {
                    return false;
                }

                if (secondByte < 0x80)
                {
                    return false;
                }

                int len = secondByte & 0x7F;
                int nextByte = 2;
                if (len == 126)
                {
                    byte[] lenByte = new byte[2];
                    lenByte[0] = Buffer[2];
                    lenByte[1] = Buffer[3];
                    len = BitConverter.ToUInt16(lenByte, 0);
                    nextByte = 4;
                }

                if (len == 127)
                {
                    byte[] lenByte = new byte[4];
                    lenByte[0] = Buffer[2];
                    lenByte[1] = Buffer[3];
                    lenByte[2] = Buffer[4];
                    lenByte[3] = Buffer[5];
                    len = BitConverter.ToUInt16(lenByte, 0);
                    nextByte = 6;
                }

                byte[] mask = new byte[4];
                byte[] text = new byte[len];
                Array.Copy(Buffer, nextByte, mask, 0, 4);
                Array.Copy(Buffer, nextByte + 4, text, 0, len);

                byte[] unmaskedText = new byte[text.Length];

                for (var i = 0; i < text.Length; i++)
                {
                    unmaskedText[i] = Convert.ToByte(text[i] ^ mask[i % 4]);
                }

                string t = Encoding.UTF8.GetString(unmaskedText);

                inPacket.Buffer = Convert.FromBase64String(t);
                inPacket.Position = 0;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        new public void Send(byte[] message)
        {
            Send(message, false);
        }

        new public void Send(byte[] message, bool force)
        {
            try
            {
                String strMessage = Convert.ToBase64String(message);
                byte[] sendText = Encoding.UTF8.GetBytes(strMessage);
                byte[] temp;
                if (sendText.Length > 125)
                {
                    if (sendText.Length < 65536)
                    {
                        temp = new byte[4 + sendText.Length];
                    }
                    else
                    {
                        temp = new byte[10 + sendText.Length];
                    }
                }
                else
                {
                    temp = new byte[2 + sendText.Length];
                }
                temp[0] = 0x81;

                if (sendText.Length > 125)
                {
                    if (sendText.Length < 65536)
                    {
                        temp[1] = 126;
                        temp[2] = Convert.ToByte(sendText.Length >> 8);
                        temp[3] = Convert.ToByte(sendText.Length & 0xFF);
                        Array.Copy(sendText, 0, temp, 4, sendText.Length);
                    }
                    else
                    {
                        temp[1] = 127;
                        byte[] len = new byte[8];
                        len = BitConverter.GetBytes((long)sendText.Length);
                        byte[] tt = new byte[8];
                        for (var i = 0; i < 8; i++)
                        {
                            tt[7 - i] = len[i];
                        }
                        Array.Copy(tt, 0, temp, 2, 8);
                        Array.Copy(sendText, 0, temp, 10, sendText.Length);
                    }
                }
                else
                {
                    temp[1] = Convert.ToByte(sendText.Length);
                    Array.Copy(sendText, 0, temp, 2, sendText.Length);
                }

                socket.Send(temp);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
