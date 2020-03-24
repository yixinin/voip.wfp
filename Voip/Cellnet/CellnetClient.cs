using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;

namespace Voip.Cellnet
{
    public class CellnetClient
    {
        private Socket _tcpSocker;
        public Socket TcpSocker
        {
            get
            {
                return this._tcpSocker;
            }
            set
            {
                this._tcpSocker = value;
            }
        }

        private readonly int HEADER_SIZE = 4;

        public bool IsConnected
        {
            get
            {
                if (_tcpSocker == null)
                {
                    return false;
                }
                return _tcpSocker.Connected;
            }
        }

        public string Host { get; private set; }
        public int Port { get; private set; }

        //[ComVisible(true)]
        //public delegate void OnClosedEventHandler(Socket sender, WebSocketClosedEventArgs args);
        //public event OnClosedEventHandler OnClosed;



        [ComVisible(true)]
        public delegate void OnMessageEventHandler(Socket sender, SocketMessageEventArgs args);

        public event OnMessageEventHandler OnMessage;

        public CellnetClient(string host, int port)
        {
            this._tcpSocker = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //this.Address = address;
            this.Host = host;
            this.Port = port;
        }

        public void Connect(string token)
        {
            if (this._tcpSocker == null)
            {
                this._tcpSocker = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
            if (this.Host == "" || this.Port == 0)
            {
                return;
            }

            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(this.Host), this.Port);
                this._tcpSocker.ConnectAsync(endPoint).ContinueWith(_Activator =>
                {
                    _Activator.Wait();
                    if (_tcpSocker.Connected)
                    {
                        Task.Run(() =>
                        {
                            ReceiveMessage();
                        });
                        //发送一条空消息
                        Send(new Protocol.EchoReq
                        {
                            Header = new Protocol.ReqHeader { Token = token }
                        });
                    }

                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("socket connect fail, ex:", ex);
            }
        }



        public void Close()
        {
            if (_tcpSocker.Connected)
            {
                this._tcpSocker.Close();
            }

        }

        public void Dispose()
        {
            if (_tcpSocker != null)
            {

            }
            this._tcpSocker.Dispose();
        }

        public void Send<T>(T message) where T : pb::IMessage<T>
        {
            try
            {

                byte[] data = new byte[message.CalculateSize()];
                using (var stream = new CodedOutputStream(data))
                {
                    message.WriteTo(stream);
                    var buffer = new byte[data.Length + 4];
                    //var bs = new byte[data.Length + 2];

                    var hashid = Utils.Cellnet.StringHash(message.GetType().FullName.ToLower());
                    var ids = Utils.Cellnet.IntToBitConverter(hashid);
                    var lens = Utils.Cellnet.IntToBitConverter((UInt16)(data.Length + HEADER_SIZE - 2));
                    for (var i = 0; i < 2; i++)
                    {
                        buffer[i] = lens[i];
                    }

                    for (var i = 2; i < 4; i++)
                    {
                        buffer[i] = ids[i - 2];
                    }



                    for (var i = 4; i < buffer.Length; i++)
                    {
                        buffer[i] = data[i - 4];
                    }
                    if (_tcpSocker.Connected)
                    {
                        var n = _tcpSocker.Send(buffer);
                        if (n != buffer.Length)
                        {
                            Debug.WriteLine(string.Format("send msg not complete, n={0}, expect={1}", n, buffer.Length));
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("send message ex: {0}", ex));
            }
        }


        private void ReceiveMessage()
        {
            while (_tcpSocker.Connected)
            {

                try
                {
                    var header = new byte[HEADER_SIZE];
                    var n = this._tcpSocker.Receive(header);
                    if (n == 0)
                    {
                        Task.Delay(10).Wait();
                        continue;
                    }
                    if (n != HEADER_SIZE)
                    {
                        Debug.WriteLine("read msg header fail");
                        continue;
                    }

                    var lens = new byte[2];
                    for (var i = 0; i < 2; i++)
                    {
                        lens[i] = header[i];
                    }

                    var ids = new byte[2];
                    if (header.Length >= 4)
                    {
                        ids[0] = header[2];
                        ids[1] = header[3];
                    }
                    var msgId = BitConverter.ToUInt16(ids, 0);

                    var len = Utils.Cellnet.BitToShort(lens);
                    var message = new byte[len - 2];
                    n = _tcpSocker.Receive(message);
                    if (n != message.Length)
                    {
                        Debug.WriteLine("read msg body fail" + Utils.Bytes.GetBufferText(header));
                        continue;
                    }
                    var args = new SocketMessageEventArgs();

                    var buf = new byte[HEADER_SIZE + message.Length];
                    var buflens = Utils.Bytes.Uint32ToBytes(message.Length);
                    for (var i = 0; i < buf.Length; i++)
                    {
                        if (i < HEADER_SIZE)
                        {
                            buf[i] = buflens[i];
                        }
                        else
                        {
                            buf[i] = message[i - header.Length];
                        }
                    }

                    args.MessaeId = msgId;
                    args.Buffer = message;
                    OnMessage?.Invoke(_tcpSocker, args);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("ReceiveMessage ex:{0}", ex));
                }

            }

            //}

        }
    }

    public class SocketMessageEventArgs
    {
        public UInt16 MessaeId { get; set; }
        public byte[] Buffer { get; set; }
    }
}
