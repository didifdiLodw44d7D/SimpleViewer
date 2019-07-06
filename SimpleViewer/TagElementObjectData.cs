using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleViewer
{
    public class TagElementObjectData
    {
        public byte[] tag = new byte[2];
        public byte[] element = new byte[2];
        public byte[] vr = new byte[2];
        public byte[] length = new byte[4];
        public byte[] data = new byte[64];
    }
}
