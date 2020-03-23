using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class Config
    {
        public static string HOST = "127.0.0.1";
        //public static string HOST = "10.0.0.218";
        public static short HTTP_PORT = 8080;
        public static short TCP_PORT = 8180;
        public static string MSG_ROUTE = "/livechat/msg";

        public static string HttpMsgAddr
        {
            get
            {
                return String.Format("http://{0}:{1}{2}", HOST, HTTP_PORT, MSG_ROUTE);
            }
        }
        public static string HttpAddr
        {
            get
            {
                return String.Format("http://{0}:{1}", HOST, HTTP_PORT);
            }
        }

    }
}
