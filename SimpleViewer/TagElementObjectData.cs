using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleViewer
{
    class TagElementObjectData
    {
        public byte[] tag = new byte[4];
        public byte[] element = new byte[4];
        public byte[] vr = new byte[2];
        public byte[] lenght = new byte[4];
        public byte[] data = new byte[128];
    }
}
