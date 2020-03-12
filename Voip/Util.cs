using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class Util
    {
        public static UInt32 BytesToUint32(byte[] bs)
        {
            return 0;
        }
        public static int GetBufSize(byte[] header)
        {
            var len = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                len[i] = header[i + 2];
            }
            return (int)BytesToUint32(len);
        }
    }
}
