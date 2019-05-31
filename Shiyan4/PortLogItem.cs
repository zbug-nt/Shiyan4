using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiyan4
{
    class PortLogItem
    {
        public bool IsRecv { get; }
        public byte[] Data { get; }

        public PortLogItem(MidiMessage msg)
        {
            Data = msg.DataBytes;
            IsRecv = msg.IsRecv;
        }
    }
}
