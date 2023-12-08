/*
 * Created by SharpDevelop.
 * User: emoreno
 * Date: 1/20/2016
 * Time: 4:49 PM
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
		public MainHub main_hub = null;
		
		private const byte PC_SA = 0xF9;
        private bool rx_thread_exit = false;
        private bool tx_thread_exit = false;
        private bool can_init = false;
        private int  can_init_delay = 0;		// ms before next can init
        private bool memory_response = false;
        private j1939tp tx_j1939_tp;
        private List<j1939tp> j1939TpRxList = null;

        // memory access commands
        private const uint MEM_CMD_ERASE = 0;
        private const uint MEM_CMD_READ = 1;
        private const uint MEM_CMD_WRITE = 2;
        private const uint MEM_CMD_STATUS_REQ = 3;
        private const uint MEM_CMD_OP_COMPLETE = 4;
        private const uint MEM_CMD_OP_FAIL = 5;
        private const uint MEM_CMD_BOOT_LOAD = 6;
        private const uint MEM_CMD_EDCP_GEN = 7;

        // memory spaces
        private const uint MEM_SPACE_FLASH = 0;
        private const uint MEM_SPACE_EEPROM = 1;
        private const uint MEM_SPACE_APP_CKS = 2;
        private const uint MEM_SPACE_PN_SN = 3;

        // globals
        private string strMffReply = "";
        public List<int> SrcAddrList = null;
        public  int ModuleAddr = 0x80;

		private Object _mutex_can_rcv_que = null;				// file data locking
		private Queue<PortPackage> CanRcvQue = null;


		public void InitCanQue()
		{
			CanRcvQue = new Queue<PortPackage>();
			_mutex_can_rcv_que = new Object();
		}
        

        public CanHub(MainHub MyMainHub)
        {
        	main_hub = MyMainHub;
            CheckForDLL();
            InitCanQue();
            SrcAddrList = new List<int>();
            // start the receive thread
            Thread rx_thread = new Thread(new ThreadStart(RxThread));
            rx_thread.Priority = ThreadPriority.AboveNormal;
            rx_thread.Start();
            // start the transmit thread
            Thread tx_thread = new Thread(new ThreadStart(TxThread));
            tx_thread.Priority = ThreadPriority.Highest;
            tx_thread.Start();
            tx_j1939_tp = new j1939tp();
            j1939TpRxList = new List<j1939tp>();
            j1939TpRxList.Add(new j1939tp());
            j1939TpRxList.Add(new j1939tp());
            j1939TpRxList.Add(new j1939tp());
        }



        public void ShutDown()
		{
            rx_thread_exit = true;    // kill the receive thread
            tx_thread_exit = true;    // kill the transmit thread
            PCAN_USB.Close();         // shutdown CAN
		}


        private void CheckForDLL()
        {
            string Dll_FileName = Application.StartupPath + "\\PCAN_USB.dll";
            if(File.Exists(Dll_FileName)==false) {
                MessageBox.Show("Missing file " + Dll_FileName);
            }
        }
        
        // copy byte[] until char match
        private byte[] CopyToMatch(byte[] srcBytes,char EndMatch)
        {
            int dstLen=1;
            int srcLen = srcBytes.Length;
            int cnt;
            for(cnt=0; cnt<srcLen; cnt++) {
                if(srcBytes[cnt]==EndMatch) {
                    dstLen = cnt;
                    break;
                }
            }
            byte[] dstBytes = new byte[dstLen];
            for(cnt=0; cnt<dstLen; cnt++) {
                dstBytes[cnt] = srcBytes[cnt];
            }
            return (dstBytes);
        }


        // copy srcBytes for length bytes
        private byte[] CopyToLength(byte[] srcBytes, int dstLen)
        {
            int srcLen = srcBytes.Length;
            int cnt;
            byte[] dstBytes = new byte[dstLen];
            for(cnt=0; cnt<dstLen; cnt++) {
                dstBytes[cnt] = srcBytes[cnt];
            }
            return(dstBytes);
        }


		// called every 100 ms by main hub
		private int CanTics = 0;
        public void TicTimer()
        {
        	if(main_hub.EnablePcanFlag) {
				if(can_init) {
	        		gwTimer();											// transmit timer
        		}
        		else {
		        	SrcAddrList.Clear();
	        		main_hub.CanAddressQue.Enqueue(SrcAddrList);	// Report Connected Can Address List
        		}
		        CanTics++;
		        if(CanTics >= 20) {										// 2 seconds
		        	CanTics = 0;
		        	if(WriteInProgress == false) {
		        		main_hub.CanAddressQue.Enqueue(SrcAddrList);	// Report Connected Can Address List
		        		SrcAddrList.Clear();
		        		if(can_init) {
		        			RequestAddrClaim();							// request address claim from all units
		        		}
		        	}
        		}
        	}
        }



        // interface from MsdView portControl
        public bool canSendSerial(string strPgn)
        {
        	if(tx_j1939_tp.state == j1939tp.STATE_IDLE) {
        		if(gw_response == GW_RESP_NONE) {
		        	gwProcessMFF(strPgn);
		        	return(true);
        		}
        	}
        	return(false);
        }


        // interface to MsdView portControl
        public string canReadSerial()
        {
        	PortPackage MyMsg = RemoveFromCanRcvQue();
        	if(MyMsg != null) {
        		return(MyMsg.MsgData);
        	}
        	return("");
        }


		// Send Message to ConnectList
        public void MsgToProduct(string strPortName, string strType, string strMsg)
		{
			if((main_hub != null) && (main_hub.PortQue != null)) {
        		AddToCanRcvQue("CAN" + strPortName, strType, strMsg);
			}
		}


        public void AddToCanRcvQue(string strPortName, string strType, string strData)
		{
			lock(_mutex_can_rcv_que)  {
				if(CanRcvQue!=null) {
					CanRcvQue.Enqueue(new PortPackage(strPortName, strType, strData));
				}
			}
		}


        public PortPackage RemoveFromCanRcvQue()
		{
			PortPackage MyMsg = null;
			lock(_mutex_can_rcv_que)  {
				if(CanRcvQue!=null) {
					if(CanRcvQue.Count > 0) {
						MyMsg = CanRcvQue.Dequeue();
					}
				}
			}
			return(MyMsg);
		}
       

	}
}
