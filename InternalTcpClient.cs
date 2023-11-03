using System;
using System.Net;
using System.Net.Sockets;

namespace LocalServer {
    public partial class TcpServer {
        private class InternalTcpClient {
            public event MessageHandler MessageEvent;
            public event EventHandler_PacketReceived PacketReceivedEvent;

            public delegate void EventHandler_ConnectionFailure(object sender);
            public event EventHandler_ConnectionFailure ConnectionFailureEvent;

            private Socket _socket;
            private string _ip;
            private int _port;
            private const int BUFFER_SIZE = 4096;

            private IPEndPoint _endPoint;

            public InternalTcpClient(Socket client) {
                try {
                    _socket = client;

                    var clientIP = (IPEndPoint)_socket.RemoteEndPoint;
                    _endPoint = clientIP;
                    _ip = clientIP.Address.ToString();
                    _port = clientIP.Port;

                    var socketArgs = new SocketAsyncEventArgs();
                    socketArgs.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);
                    socketArgs.UserToken = _socket;
                    socketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(PacketReceived);
                    _socket.ReceiveAsync(socketArgs);
                }
                catch(Exception ex) {
                    MessageEvent(this, "EXCEPTION", ex.Message);
                }
            }

            public string IP {
                get { return _ip; }
            }

            public int Port {
                get { return _port; }
            }

            public IPEndPoint endPoint { 
                get { return _endPoint; } 
            }

            public void Close() {
                try {
                    if (_socket != null && _socket.Connected) {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
                }
                catch(Exception ex) {
                    MessageEvent(this, "EXCEPTION", ex.Message);
                }
            }

            public bool Send(byte[] packet, int packetLen) {
                byte[] buffer = new byte[packetLen];

                try {                     
                    Buffer.BlockCopy(packet, 0, buffer, 0, packetLen);
                    _socket.Send(buffer);
                }
                catch(Exception ex) {
                    MessageEvent(this, "EXCEPTION", ex.Message);
                    if (ConnectionFailureEvent != null) ConnectionFailureEvent(this);
                    return false;
                }
                return true;
            }

            private void PacketReceived(object sender, SocketAsyncEventArgs e) {
                Socket socket;
                byte[] packet;

                try {
                    if(_socket.Connected && e.BytesTransferred > 0) {

                        packet = new byte[e.BytesTransferred];
                        Buffer.BlockCopy(e.Buffer, 0, packet, 0, e.BytesTransferred);
                        socket = (Socket)sender;

                        PacketReceivedEvent(this, packet, (IPEndPoint)socket.RemoteEndPoint);
                        socket.ReceiveAsync(e);
                    }
                }
                catch(Exception ex) {
                    MessageEvent(this, "EXCEPTION", ex.Message);
                }
            }
        }
    }
}
