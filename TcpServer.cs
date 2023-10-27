using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LocalServer {
    public partial class TcpServer {
        public delegate void MessageHandler(object sender, string type, string message);
        public event MessageHandler MessageEvent;

        public delegate void EventHandler_ClientAccepted(object sender, object eventArgs);
        public event EventHandler_ClientAccepted ClientAcceptedEvent;

        public delegate void EventHandler_PacketReceived(object sender, byte[] packet, IPEndPoint endPoint);
        public event EventHandler_PacketReceived PacketReceivedEvent;

        private Socket _socket;
        private int _port;

        private List<InternalTcpClient> _connectedClientList;

        public TcpServer(int port) {
            _port = port;
            _connectedClientList = new List<InternalTcpClient>();
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
            catch (Exception ex) {
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
            catch (Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
            }
        }

        public bool Send(string packet, IPEndPoint destination) {
            byte[] buffer;
            bool res = false;
            InternalTcpClient client;

            try {
                buffer = Encoding.Default.GetBytes(packet);
                client = GetRemoteClient(destination);
                if(client == null) {
                    MessageEvent(this, "ERR", "EndPoint - " + destination.ToString() + " is not connected");
                    return false;
                }
                res = client.Send(buffer, buffer.Length);
            }
            catch(Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
                return false;
            }

            return res;
        }

        public bool Send(byte[] packet, int packetLen, IPEndPoint destination) {
            bool res = false;
            InternalTcpClient client;

            try {
                client = GetRemoteClient(destination);
                if (client == null) {
                    MessageEvent(this, "ERR", "EndPoint - " + destination.ToString() + " is not connected");
                    return false;
                }
                res = client.Send(packet, packetLen);
            }
            catch (Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
                return false;
            }

            return res;
        }

        private InternalTcpClient GetRemoteClient(IPEndPoint endPoint) {
            InternalTcpClient remoteClient = null;

            try {
                foreach (var client in _connectedClientList) {
                    if (client.endPoint.Equals(endPoint)) {
                        remoteClient = client;
                        break;
                    }
                }
            }
            catch (Exception ex) {
                MessageEvent(this, "EXCEPTION", ex.Message);
                return null;
            }

            return remoteClient;
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
                    client.MessageEvent += MessageEvent;
                    client.PacketReceivedEvent += PacketReceivedEvent;

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

        private void Client_PacketReceivedEvent(object sender, byte[] packet, IPEndPoint endPoint) {
            throw new NotImplementedException();
        }
    }
}
