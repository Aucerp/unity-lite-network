using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Unet.LitUdp
{
    public class UdpReceiver : IDisposable
    {
        private Socket receiverSocket;
        private readonly int port;
        private readonly int bufferSize;
        private byte[] buffer;
        private bool isRunning;
        private Thread receiveThread;
        private bool disposed;

        public event Action<string> OnMessageReceived;
        public event Action<Exception> OnError;

        public UdpReceiver(int port = 11000, int bufferSize = 1024)
        {
            this.port = port;
            this.bufferSize = bufferSize;
        }

        public void Start()
        {
            if (disposed) throw new ObjectDisposedException(nameof(UdpReceiver));
            if (isRunning) return;

            try
            {
                receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                receiverSocket.Bind(endPoint);

                buffer = new byte[bufferSize];
                isRunning = true;

                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                throw;
            }
        }

        private void ReceiveData()
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                try
                {
                    int received = receiverSocket.ReceiveFrom(buffer, ref sender);
                    if (received > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, received);
                        if (OnMessageReceived != null)
                        {
                            OnMessageReceived(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        if (OnError != null)
                        {
                            OnError(ex);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Stop();
                if (receiverSocket != null)
                {
                    receiverSocket.Close();
                    receiverSocket = null;
                }

                if (receiveThread != null && receiveThread.IsAlive)
                {
                    receiveThread.Join();
                }
                disposed = true;
            }
        }
    }
} 