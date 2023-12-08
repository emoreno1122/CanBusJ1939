/*
 * Created by SharpDevelop.
 * User: emoreno
 * Date: 2/3/2016
 * Time: 11:23 AM
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
		public const int  USB_PGN_MFF_REQ =		    0x0101;
		public const int  USB_PGN_MFF_ID =          0x0102;
		public const int  USB_PGN_MFF_MON_LIST =    0x0103;
		public const int  USB_PGN_MFF_UNIT_LIST =   0x0104;
		public const int  USB_PGN_MFF_TAB_TEXT =    0x0105;
		public const int  USB_PGN_MFF_SETTINGS =	0x0106;
		public const int  USB_PGN_MFF_TABLE_SCN =   0x0107;
		public const int  USB_PGN_MFF_PLOT_SCN =    0x0108;
		public const int  USB_PGN_MFF_ALERT_TEXT =  0x0109;
		public const int  USB_PGN_MFF_END =         0x010A;
		public const int  USB_PGN_MFF_MODULE =		0x0119;
		public const int  USB_PGN_MFF_MODULE_END =	0x011A;

		public const int  USB_PGN_XFER_REQ =		0x0201;
		public const int  USB_PGN_XFER_ID =         0x0202;
		public const int  USB_PGN_XFER_DATA =       0x0203;
		public const int  USB_PGN_XFER_DATA_END =   0x0204;
		public const int  USB_PGN_XFER_MON =        0x0205;
		public const int  USB_PGN_XFER_ALERT =      0x0206;
		public const int  USB_PGN_XFER_TEST =       0x0207;
		public const int  USB_PGN_XFER_NOTES =	 	0x0209;
		
		public const int  USB_PGN_BOOT_REQ =		0x0301;
		public const int  USB_PGN_BOOT_WRITE =		0x0302;
		public const int  USB_PGN_CKS_WRITE =		0x0304;
		
		public const int  USB_PGN_DAQ_REQ =            0x0501;
		public const int  USB_PGN_DAQ_REC_NAME_DONE =  0x050B;
		public const int  USB_PGN_DAQ_READ_FILE =      0x050C;
		public const int  USB_PGN_DAQ_XFER_FILE =      0x050D;
		public const int  USB_PGN_DAQ_XFER_FILE_DONE = 0x050E;
		public const int  USB_PGN_DAQ_XFER_FILE_ERR =  0x050F;
		public const int  USB_PGN_DAQ_DEL_FILE =       0x0510;
		public const int  USB_PGN_DAQ_FILE_DEL_RES =   0x0511;
		public const int  USB_PGN_DAQ_FS_INFO =        0x0512;
		public const int  USB_PGN_DAQ_REC_NAME =       0x0513;
		
		/* USB MFF request definitions */
		
		public const int USB_MFF_REQ_PROD_ID =         0x01;
		public const int USB_MFF_REQ_TABS =            0x02;
		public const int USB_MFF_REQ_APP_EXIT =        0x03;
		public const int USB_MFF_REQ_MODULES =		   0x04;
		
		/* USB XFER request definitions */
		
		public const int USB_XFER_REQ_MON =            0x01;
		public const int USB_XFER_REQ_DATA_ID =        0x02;
		public const int USB_XFER_REQ_DATA =           0x03;
		public const int USB_XFER_REQ_SET_DEFAULTS =   0x04;
		public const int USB_XFER_REQ_CLEAR_ALERTS =   0x05;
		public const int USB_XFER_REQ_COMP_DEFAULT =   0x06;
		
		/* USB BOOT request definitions */
		
		public const int USB_BOOT_REQ_EXIT_TO_APP =	   0x01;
		public const int USB_BOOT_REQ_CKS =		       0x02;
		public const int USB_BOOT_REQ_PN_SN	=	       0x03;
		public const int USB_BOOT_REQ_APP_RANGE =	   0x05;
		
		/* USB DAQ request definitions */
		
		public const int USB_DAQ_REQ_REC_NAMES =       0x13;
		public const int USB_DAQ_REQ_FS_INFO =         0x12;
		public const int USB_DAQ_REQ_XFER_CANCEL =	   0x14;
		public const int USB_DAQ_REQ_XFER_PAUSE =	   0x15;
		public const int USB_DAQ_REQ_XFER_RESUME =	   0x16;
		
		/* USB settings type definitions */
		
		public const int USB_SETTINGS_TYPE_NONE	=	   0;
		public const int USB_SETTINGS_TYPE_U8 =	       1;
		public const int USB_SETTINGS_TYPE_U16 =	   2;
		public const int USB_SETTINGS_TYPE_S16 =	   3;
		
		/* USB alerts time definitions */
		
		public const int USB_ALERT_UPDATE_PERIOD =	   10000;
		public const int USB_MIN_ALERT_PERIOD =		    1000;
		
		/* USB disconnect time definition */
		
		public const int USB_DISCONNECT_TIME =		    2000;		// 2 seconds
		public const int USB_DEAD_TIMEOUT =		        1000;		// 1 second
		
		/* CAN communication type setting number */
		public const int COMM_TYPE_INDEX =		        318;		// When this setting changes, CAN is re-initialized with the appropriate parameters (i.e. PROIII or VNET)
		

	    public const int MEM_ERASE       = 0;
    	public const int MEM_READ        = 1;
    	public const int MEM_WRITE       = 2;
    	public const int MEM_STATUS_REQ  = 3;
    	public const int MEM_OP_COMPLETE = 4;
    	public const int MEM_OP_FAIL     = 5;
    	public const int MEM_BOOT_LOAD   = 6;
    	public const int MEM_EDCP_GEN    = 7;
		

    	public const uint GW_RESP_NONE         = 0;
	    public const uint GW_RESP_EXIT_TO_BOOT = 1;
	    public const uint GW_RESP_COMP_ID      = 2;
	    public const uint GW_RESP_ERASE        = 3;
	    public const uint GW_RESP_WRITE        = 4;
	    public const uint GW_RESP_CALC_CKS     = 5;
	    public const uint GW_RESP_CKS_WRITE    = 6;

		public const uint TX_ACTIVE            = 0;
		public const uint TX_QUIET             = 1;
    	
    	
		public uint gw_response = GW_RESP_NONE;
		public uint gw_response_mod = 0x00ff;
		public uint[] gw_buff = new uint[16];
		public uint gw_tmr = 0;
		public uint gw_tmr_limit = 5;		// 5 * 100ms = 500ms
		public bool gw_flash_in_prog = false;
		public UInt32 gw_addr = 0;
		
		public bool WriteInProgress = false;
    	

		// called every 100ms
		public void gwTimer()
		{
			if((gw_response != GW_RESP_NONE) || (tx_j1939_tp.state != j1939tp.STATE_IDLE)) {
				gw_tmr++;
				if(gw_tmr > gw_tmr_limit) {					// 0.5 sec or 10 sec time out
					gw_tmr = 0;
					tx_j1939_tp.state = j1939tp.STATE_IDLE;
					gw_response = GW_RESP_NONE;
					gw_flash_in_prog = false;
					SendDM13(TX_ACTIVE);
					gw_tmr_limit = 5;						// 0.5 sec time out
				}
			}
		}


   	
		// message from MsdView to external CAN module		
		public void gwProcessMFF (string strMsg)
		{
			uint mod;					// module address (0x80, 0x81)
			int Pgn;
			int req;
			j1939msg msg = new j1939msg();
			bool pass_thru = true;
			int i;

			if((strMsg.StartsWith("H")) && (strMsg.EndsWith("\n"))) {
				gw_tmr_limit = 5;											// 0.5 seconds
				mod = (uint)strToHex(strMsg, 3, 2);
				if(strMsg.Substring(5,2)=="07") {
					mod = (uint) strToHex(strMsg, 1, 2);
				}
				Pgn = strToHex(strMsg, 5, 4);
				switch(Pgn) {
					case USB_PGN_XFER_REQ:										// 0201
						req = strToHex(strMsg, 12, 2);
						if(req == USB_XFER_REQ_DATA_ID) {						// 02  Req P/N S/N
							SendReq (mod, MCAN_PGN_COMPONENT_ID);				
							gw_response = GW_RESP_COMP_ID;
							gw_response_mod = mod;
							gw_tmr = 0;
							pass_thru = false;
							gw_flash_in_prog = true;
						}
						break;
					case USB_PGN_MFF_REQ:										// 0101
						req = strToHex(strMsg, 12, 2);
						if (req == USB_MFF_REQ_APP_EXIT) {						// 03	Application exit
							/* translate to DM14 request */
							SendDM14 (mod, 0, 0, MEM_BOOT_LOAD, MEM_SPACE_FLASH);
							gw_response = GW_RESP_NONE;							// no reply expected
							gw_response_mod = mod;
							gw_tmr = 0;
							pass_thru = false;
							gw_flash_in_prog = true;
							/* now quiet the rest of the bus */
							SendDM13 (TX_QUIET);
						}
						break;
					case USB_PGN_BOOT_REQ:										// 0301
						req = strToHex(strMsg, 12, 2);
						if (req == USB_BOOT_REQ_EXIT_TO_APP) {					// 01   Exit to application
							SendDM14 (mod, 0x3000, 0, MEM_BOOT_LOAD, MEM_SPACE_FLASH);	/* translate to DM14 request */
							gw_response = GW_RESP_NONE;
							gw_response_mod = mod;
							gw_tmr = 0;
							pass_thru = false;
							gw_flash_in_prog = false;
							SendDM13 (TX_ACTIVE);
						} else if (req == USB_BOOT_REQ_PN_SN) {					// 03   Req P/N S/N
							SendReq (mod, MCAN_PGN_COMPONENT_ID);
							gw_response = GW_RESP_COMP_ID;
							gw_response_mod = mod;
							gw_tmr = 0;
							pass_thru = false;
							gw_flash_in_prog = true;
						} else if (req == USB_BOOT_REQ_CKS) {					// 02	Req Calc Application CkSum
							/* send request to calculate checksum */
							SendDM14 (mod, 0x3000, 0, MEM_EDCP_GEN, MEM_SPACE_FLASH);
							gw_response = GW_RESP_CALC_CKS;
							gw_response_mod = mod;
							gw_tmr = 0;
							pass_thru = false;
							gw_flash_in_prog = true;
						} else {
							;
						}
						break;
					case USB_PGN_BOOT_WRITE:
						gw_addr = (uint)strToHex(strMsg, 10, 8);				/* extract the address */
						gw_response_mod = mod;
						pass_thru = false;
						gw_flash_in_prog = true;
						gw_tmr = 0;
						req = 18;												/* get an index to the start of data */
						for (i=0; i<16; i++,req+=2) {							/* load the data buffer */
							uint data = (uint)strToHex(strMsg, req, 2);
							gw_buff[i] = data;
						}
						if(WriteInProgress == false) {
							SendDM14 (mod, gw_addr, 0, MEM_ERASE, MEM_SPACE_FLASH);
							gw_response = GW_RESP_ERASE;
							WriteInProgress = true;
							gw_tmr_limit = 100;				// 10 seconds
							break;
						}
						SendDM14 (mod, gw_addr, 0x10, MEM_WRITE, MEM_SPACE_FLASH);
						gw_response = GW_RESP_WRITE;
						break;
					case USB_PGN_CKS_WRITE:
						SendDM14 (mod, 0, 2, MEM_WRITE, MEM_SPACE_APP_CKS);
						gw_response = GW_RESP_CKS_WRITE;
						gw_response_mod = mod;
						gw_tmr = 0;
						pass_thru = false;
						gw_flash_in_prog = true;
						WriteInProgress = false;
						break;
					default:
						break;
				}
				if(pass_thru) {									/* no translation needed */
					msg.prio = (byte)LOW_PRIO;
					msg.pgn = (ushort)MCAN_PGN_MFF;
					msg.da = (byte)mod;
					msg.sa = PC_SA;
					msg.dlc = (ushort)strMsg.Length;
					msg.data = Encoding.ASCII.GetBytes(strMsg);
					TransmitJ1939(msg);
					WriteInProgress = false;
				}
			}
		}
		

		public Int32 strToHex(string strMsg, int startPos, int length)
		{
			int result = 0;
			if(strMsg.Length >= (startPos + length)) {
				Int32.TryParse(strMsg.Substring(startPos, length), System.Globalization.NumberStyles.HexNumber, null, out result);
			}
			return(result);
		}
		

	}
}

