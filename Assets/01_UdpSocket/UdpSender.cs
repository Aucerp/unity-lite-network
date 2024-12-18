using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Unet.LitUdp
{
    public class UdpSender : IDisposable
    {
        private Socket senderSocket;
        private readonly int port;
        private readonly string ipAddress;
        private bool disposed;

        public UdpSender(string ip = "127.0.0.1", int port = 11000)
        {
            this.ipAddress = ip;
            this.port = port;
            senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void SendMessage(string message)
        {
            if (disposed) throw new ObjectDisposedException(nameof(UdpSender));

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                senderSocket.SendTo(data, endPoint);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (senderSocket != null)
                {
                    senderSocket.Close();
                    senderSocket = null;
                }
                disposed = true;
            }
        }
    }
} 