/*
 * Created by SharpDevelop.
 * User: emoreno
 * Date: 2/2/2016
 * Time: 11:39 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Xml;
using Peak.Can.Light;
using System.Reflection;


namespace MsdEdit
{
	/// <summary>
	/// </summary>
	public partial class CanHub
	{

		
		public const uint LOW_PRIO  = 0x06;
		public const uint MED_PRIO  = 0x03;
		public const uint HIGH_PRIO = 0x00;

		/* standard J1939 PGNs */
		public const uint MCAN_PGN_ACK				= 0xE800;
		public const uint MCAN_PGN_REQUEST			= 0xEA00;
		public const uint MCAN_PGN_TP_DT			= 0xEB00;
		public const uint MCAN_PGN_TP_CM			= 0xEC00;
		public const uint MCAN_PGN_ADDR_CLAIM		= 0xEE00;
		public const uint MCAN_PGN_MFF				= 0xEF00;
		public const ushort MCAN_PGN_COMPONENT_ID	= 0xFEEB;
		public const uint MCAN_PGN_SOFTWARE_ID		= 0xFEDA;
		public const uint MCAN_PGN_DM13		   		= 0xDF00;
		public const uint MCAN_PGN_DM14				= 0xD900;
		public const uint MCAN_PGN_DM15				= 0xD800;
		public const uint MCAN_PGN_DM16				= 0xD700;
		public const uint MCAN_PGN_MFF_REQ_DATA		= 0xD600;
		public const uint MCAN_PGN_MFF_REQ_GUI		= 0xD500;
		public const uint MCAN_PGN_CUSTOM_CONTROL	= 0xD400;


		/* MSD-CAN source address definitions */
		public const uint MCAN_SA_DC				= 0x28;
		public const uint MCAN_SA_MFC				= 0x2B;
		public const uint MCAN_SA_NULL				= 0xFE;
		public const uint MCAN_SA_GLOBAL			= 0xff;


		private void SendMFFString(string mffmsg, uint da)
        {
            strMffReply = "";                      // clear reply string 
            char[] carray = mffmsg.ToCharArray();
            j1939msg msg = new j1939msg();
            msg.data = new byte[carray.Length];

            msg.prio = 0x18;
            msg.pgn = 0xEF00;
            msg.da = (byte)da;
            msg.sa = PC_SA;
            msg.dlc = (ushort)carray.Length;
            for(int i=0; i<carray.Length; i++)  {
                msg.data[i] = (byte)carray[i];
            }
            TransmitJ1939(msg);
            WaitForReply(200);                       // wait up to 200ms for reply
        }


		private void WaitForReply(int MaxMs)
        {
            DateTime StartTime = DateTime.Now;
            while(true) {
                TimeSpan Ts = DateTime.Now - StartTime;
                if(Ts.TotalMilliseconds>200) break;               // wait up to 200ms for reply
                if(strMffReply.Length>0) {
                	break;
                }
            }
        }


		
		private void SendReq(uint da, uint pgn)
		{
            j1939msg msg = new j1939msg();
		
            msg.prio = (byte)LOW_PRIO;
            msg.pgn = (ushort)MCAN_PGN_REQUEST;
            msg.da = (byte)da;
			msg.sa = PC_SA;
			msg.dlc = 3;
			msg.data[0] = (byte)(pgn & 0xff);
			msg.data[1] = (byte)((pgn >> 8) & 0xff);
			msg.data[2] = 0;
            TransmitJ1939(msg);
		}
		
		
		
		// start / stop broadcast messages
        private void SendDM13(uint status)
        {
            j1939msg msg = new j1939msg();
		    uint i;
		
		    msg.prio = (byte)HIGH_PRIO;
		    msg.pgn = (ushort)MCAN_PGN_DM13;
		    msg.da = (byte)MCAN_SA_GLOBAL;
		    msg.sa = PC_SA;
		    msg.dlc = 8;
		    /* set the network status */
		    if (status == TX_QUIET) {
				msg.data[0] = 0x00;
		    } else {
				msg.data[0] = 0x40;
		    }
		    /* populate unused data */
		    for (i=1; i<8; i++) {
				msg.data[i] = 0xff;
		    }
            TransmitJ1939(msg);
        }


        private void SendDM14(uint mod, uint addr, uint len, uint cmd, uint space)
        {
            j1939msg msg = new j1939msg();

            msg.prio = 0;
            msg.pgn = 0xD900;
            msg.da = (byte)mod;
            msg.sa = PC_SA;
            msg.dlc = 6;
            msg.data[0] = (byte)(len & 0x00FF);
            msg.data[1] = (byte)(cmd << 1);
            msg.data[1] |= (byte)((len & 0x0700) >> 3);
            msg.data[2] = (byte)addr;
            msg.data[3] = (byte)(addr >> 8);
            msg.data[4] = (byte)(addr >> 16);
            msg.data[5] = (byte)space;
            memory_response = false;
            TransmitJ1939(msg);
        }

        

		public void RequestAddrClaim()
		{
            j1939msg msg = new j1939msg();
			/* send the global request for address claim */
			msg.prio = 6;
			msg.pgn = 0xEA00;
			msg.da = 0xFF;
			msg.sa = PC_SA;
			msg.dlc = 3;
			msg.data[0] = 0x00;
			msg.data[1] = 0xEE;
			msg.data[2] = 0x00;
            TransmitJ1939(msg);
		}


        private bool WaitDM15(int time)
        {
            bool res = false;

            time *= 20;

            // wait for response
            for (int i = 0; i < time; i++) {
                if (memory_response == false) {
                    Thread.Sleep(5);
                }
                else {
                    res = true;
                    break;
                }
            }

            return res;
        }



	}

}