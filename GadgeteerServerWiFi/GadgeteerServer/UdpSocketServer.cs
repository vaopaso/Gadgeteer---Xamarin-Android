using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace GadgeteerServer
{
    public class UdpSocketServer
    {
        public const int DEFAULT_SERVER_PORT = 5123;

        private int port;
        private Socket socket;

        public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
        public event DataReceivedEventHandler DataReceived;

        /// <summary>
        /// The default port is 5123
        /// </summary>
        public UdpSocketServer()
            : this(DEFAULT_SERVER_PORT)
        { }

        public UdpSocketServer(int port)
        {
            this.port = port;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Creates a new thread that waits for messages
        /// </summary>
        public void Start()
        {
            new Thread(StartServerInternal).Start();
        }

        private void StartServerInternal()
        //When data are available, we read them using the Socket.ReceiveFrom method.
        //Then, we create an object of type DataReceivedEventArgs,
        //using the sender endpoint and the received bytes.
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            socket.Bind(endPoint);

            while (true)
            {
                if (socket.Poll(-1, SelectMode.SelectRead))
                {
                    byte[] buffer = new byte[socket.Available];
                    int bytesRead = socket.ReceiveFrom(buffer, ref endPoint);
                    string ss = endPoint.ToString();
                    IPAddress ip = IPAddress.Parse(ss.Split(':')[0]);
                    endPoint = new IPEndPoint(ip, port);
                    DataReceivedEventArgs args = new DataReceivedEventArgs(endPoint, buffer);
                    OnDataReceived(args);

                    if (args.ResponseData != null)
                        Thread.Sleep(500);
                        socket.SendTo(args.ResponseData, endPoint);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Raises the DataReceived event
        /// </summary>
        private void OnDataReceived(DataReceivedEventArgs e)
        {
            if (DataReceived != null)
                DataReceived(this, e);
        }

        public static string BytesToString(byte[] bytes)
        {
            int length = bytes.Length;
            char[] text = new char[length];
            for (int i = 0; i < length; i++)
                text[i] = (char)bytes[i];

            return new string(text);
        }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// EndPoint of the sender who made the request
        /// </summary>
        public EndPoint RemoteEndPoint { get; private set; }
        public byte[] Data { get; private set; }

        /// <summary>
        /// Data that will be sent as reply
        /// </summary>
        public byte[] ResponseData { get; set; }

        public DataReceivedEventArgs(EndPoint remoteEndPoint, byte[] data)
        {
            RemoteEndPoint = remoteEndPoint;
            if (data != null)
            {
                Data = new byte[data.Length];
                data.CopyTo(Data, 0);
            }
        }
    }

}
