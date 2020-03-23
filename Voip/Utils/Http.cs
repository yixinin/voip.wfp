using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;

namespace Voip.Utils
{
    public class HttpClient
    {
        private readonly int HEADER_SIZE = 4;

        public System.Net.Http.HttpClient httpClient;
        public string URL;
        public HttpClient(string url)
        {
            URL = url;
            this.httpClient = new System.Net.Http.HttpClient();
        }

        public async Task<Ack> Send<Req, Ack>(Req message) 
            where Req : pb::IMessage<Req>, new()
            where Ack : pb::IMessage<Ack>, new()
        {
            try
            {
                byte[] data = new byte[message.CalculateSize()];
                using (var stream = new CodedOutputStream(data))
                {
                    message.WriteTo(stream);
                    var buffer = new byte[data.Length + 4];
                    //var bs = new byte[data.Length + 2];

                    var hashid = Cellnet.StringHash(message.GetType().FullName.ToLower());
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

                    var content = new System.Net.Http.ByteArrayContent(buffer);
                    var resp = await httpClient.PostAsync(URL, content);
                    var ackBuffer = await resp.Content.ReadAsByteArrayAsync();

                    var parser = new pb::MessageParser<Ack>(() => new Ack());
                    var ack = parser.ParseFrom(ackBuffer);
                    return ack;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("http send message ex: ", ex);
                return new Ack();
            }
        }
    }
}
