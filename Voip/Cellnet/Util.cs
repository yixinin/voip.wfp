using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Cellnet
{
    public class Utils
    {

        public static byte[] IntToBitConverter(ushort num)
        {
            return BitConverter.GetBytes(num);
        }

        public static short BitToShort(byte[] bs)
        {
            return BitConverter.ToInt16(bs, 0);
        }

        public static ushort StringHash(string s)
        {
            ushort hash = 0;
            foreach (var c in s)
            {

                var ch = (ushort)(c);


                hash = (ushort)(hash + (hash << 5) + ch + (ch << 7));

            }
            return hash;
        }
    }
}
