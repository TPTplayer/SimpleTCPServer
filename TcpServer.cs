using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace LocalServer {
    public partial class TcpServer {
        public delegate void MessageHandler(object sender, string type, string Message);
        public event MessageHandler MessageEvent;

        public delegate void EventHandler_ClientAccepted(object sender, object obj);
        public event EventHandler_ClientAccepted ClientAcceptedEvent;

        public delegate void EventHandler_PacketReceived(object sender, byte[] packet);
        public event EventHandler_PacketReceived PacketReceivedEvent;
    
        private Socket _socket;
        private int _port;

        private List<InternalTcpClient> _connectedClientList;

        public TcpServer(int port) {
            try {
                _port = port;        
                _connectedClientList = new List<InternalTcpClient>();
            }
            catch(Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
            }
        }

        public bool Open() {
            try {
                var endPoint = new IPEndPoint(IPAddress.Any, _port);
                var socketArgs = new SocketAsyncEventArgs();
                
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAccepted);
                
                _socket.Bind(endPoint);
                _socket.Listen(50);
                _socket.AcceptAsync(socketArgs);

                MessageEvent(this, "MESSAGE", IPAddress.Any + ": server open");
            }
            catch(Exception ex ) {
                MessageEvent(this, "EXCEPTION", ex.Message);
                return false;
            }
            return true;
        }

        public void Close() {
            try {
                foreach (var client in _connectedClientList) { 
                    client.Close();
                }
                _socket.Close();
            }
            catch(Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
            }
        }

        private void SocketAccepted(object sender, SocketAsyncEventArgs e) {
            InternalTcpClient client;
            
            try {
                Socket clientSocket = e.AcceptSocket;
                IPEndPoint clientEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                MessageEvent(this, "MESSAGE", "accepted: " + clientEndpoint.Address.ToString());

                if (ClientAcceptedEvent != null) ClientAcceptedEvent(sender, e);
                if (clientSocket.Connected) {
                    client = new InternalTcpClient(clientSocket);
                    _connectedClientList.Add(client);
                    MessageEvent(this, "MESSAGE", "connected: " + clientEndpoint.Address.ToString());
                }

                e.AcceptSocket = null;
                _socket.AcceptAsync(e);
            }
            catch(Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
            }
        }


    }
}
