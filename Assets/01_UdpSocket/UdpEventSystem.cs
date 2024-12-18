using System;

namespace Unet.LitUdp
{
    public static class UdpEventSystem
    {
        public static event Action<string> OnMessageReceived;
        public static event Action<Exception> OnErrorOccurred;
        public static event Action<string> OnMessageSent;

        public static void RaiseMessageReceived(string message)
        {
            if (OnMessageReceived != null)
            {
                OnMessageReceived(message);
            }
        }

        public static void RaiseErrorOccurred(Exception ex)
        {
            if (OnErrorOccurred != null)
            {
                OnErrorOccurred(ex);
            }
        }

        public static void RaiseMessageSent(string message)
        {
            if (OnMessageSent != null)
            {
                OnMessageSent(message);
            }
        }
    }
} 