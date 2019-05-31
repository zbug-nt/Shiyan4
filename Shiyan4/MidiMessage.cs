using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiyan4
{
    class MidiMessage : INotifyPropertyChanged
    {
        private static int recvIndex = 0;
        private static int sendIndex = 0;

        public int Index { get; set; }
        public string Data { get; set; }
        public bool IsRecv { get; }
        public byte[] DataBytes { get; }

        public MidiMessage(byte[] dataBytes, bool isRecv)
        {
            DataBytes = dataBytes;
            IsRecv = isRecv;
            if (isRecv) Index = ++recvIndex;
            else Index = ++sendIndex;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < dataBytes.Length; ++i)
            {
                if (i != 0) builder.Append(" ");
                builder.Append(string.Format("{0:X2}", dataBytes[i]));
            }
            Data = builder.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
