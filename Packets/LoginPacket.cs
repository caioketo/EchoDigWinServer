using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoDigWinServer.Packets
{
    public class LoginPacket
    {
        public string Nick;
        public string Password;

        public static LoginPacket Parse(Packet packet)
        {
            LoginPacket parsedPacket = new LoginPacket();
            parsedPacket.Nick = packet.GetString();
            parsedPacket.Password = packet.GetString();
            return parsedPacket;
        }
    }
}
