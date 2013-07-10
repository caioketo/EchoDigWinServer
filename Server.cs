using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace EchoDigWinServer
{
    public class Server
    {
        private Socket webListener;
        private int webPort;
        public List<WebConnection> connections = new List<WebConnection>();

        public Server(int _port)
        {
            webPort = _port;
            webListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            webListener.Bind(new IPEndPoint(IPAddress.Any, webPort));
            webListener.Listen(1000);
        }

        public void Run()
        {
            webListener.BeginAccept(new AsyncCallback(OnWebClientConnect), webListener);
        }


        private void OnWebClientConnect(IAsyncResult ar)
        {
            WebConnection connection = new WebConnection();
            connection.ListenerCallback(ar);
            connections.Add(connection);
            webListener.BeginAccept(new AsyncCallback(OnWebClientConnect), webListener);
        }
    }
}
