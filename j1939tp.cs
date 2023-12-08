using System;
using System.Collections.Generic;
using System.Text;

namespace MsdEdit
{
    class j1939tp
    {
        public const byte STATE_IDLE = 0;
        public const byte STATE_TX_DATA = 1;
        public const byte STATE_RX_DATA = 2;
        public const byte STATE_WAIT_CTS = 3;

        public uint pgn = 0;
        public byte sa = 0;
        public byte da = 0;
        public byte exp_packets = 0;
        public byte packets = 0;
        public ushort exp_bytes = 0;
        public ushort bytes = 0;
        public byte[] data = null;
        public byte[] MffData = null;
        public byte state = STATE_IDLE;
        public DateTime timestamp = DateTime.Now;

        public j1939tp()
        {
            MffData = new byte[256];
        }
    }
}
