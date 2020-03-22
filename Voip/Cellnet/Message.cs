using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;

namespace Voip.Cellnet
{
    public static class Message
    {
        public static Dictionary<UInt16, Type> messages { get; set; }


        public static void InitMessageIds()
        {
            messages = new Dictionary<ushort, Type>();

            var ts = new List<Type>();

            //group
            ts.Add(typeof(Protocol.AuthGroupAck));

            //contact
            ts.Add(typeof(Protocol.AddContactAck));
            ts.Add(typeof(Protocol.DeleteContactAck));
            ts.Add(typeof(Protocol.UpdateContactAck));
            ts.Add(typeof(Protocol.ChangePasswordAck));
            ts.Add(typeof(Protocol.ResetPasswordAck));
            ts.Add(typeof(Protocol.SearchUserAck));

            //message
            ts.Add(typeof(Protocol.SendMessageAck));
            ts.Add(typeof(Protocol.RealTimeAck));
            ts.Add(typeof(Protocol.MessageNotify));
            ts.Add(typeof(Protocol.RealTimeNotify));
            ts.Add(typeof(Protocol.CancelRealTimeAck));
            ts.Add(typeof(Protocol.PollAck));
            ts.Add(typeof(Protocol.PollMessageAck));


            //account
            ts.Add(typeof(Protocol.SignInAck));
            ts.Add(typeof(Protocol.SignUpAck));
            ts.Add(typeof(Protocol.SignOutAck));
            ts.Add(typeof(Protocol.SignOffAck));


            CacheMessage(ts);


        }

        public static void CacheMessage(List<Type> ts)
        {
            foreach (var t in ts)
            {
                var name = t.FullName.ToLower();
                var msgid = Utils.StringHash(name);
                messages[msgid] = t;
                Debug.WriteLine(string.Format("name={0}, id={1}", name, msgid));
            }

        }

        public static T Frombytes<T>(byte[] dataBytes) where T : IMessage, new()
        {
            CodedInputStream stream = new CodedInputStream(dataBytes);
            T msg = new T();
            stream.ReadMessage(msg);
            return msg;
        }

    }
}
