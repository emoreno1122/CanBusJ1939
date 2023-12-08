/*
 * Created by SharpDevelop.
 * User: dbw
 * Date: 7/20/2015
 * Time: 2:12 PM
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


namespace MsdEdit
{
    public partial class CanHub
    {
    	const int MEM_ST_PROCEED = 0;
    	const int MEM_ST_BUSY = 1;
    	const int MEM_ST_RES_1 = 2;
    	const int MEM_ST_RES_2 = 3;
    	const int MEM_ST_OP_COMP = 4;
    	const int MEM_ST_OP_FAIL = 5;
    	const int MEM_ST_RES_3 = 6;
    	const int MEM_ST_RES_4 = 7;
    	
    	int GatewayCheckSum = 0;


    	private void RxThread()
        {
            rx_thread_exit = false;
            while (rx_thread_exit == false) {
            	if(can_init_delay > 0) can_init_delay--;
				if ((can_init == false) && (can_init_delay == 0)) {
					try {
						// initialize CAN hardware
						 uint status = PCAN_USB.Init(PCAN_USB.CAN_BAUD_1M, PCAN_USB.CAN_INIT_TYPE_EX);
	                   
						
						/*---Added to check if there is a PCAN Instance open---*/
						/*if (MainHub.EnablePcanFlag) {
							status = 0;
							try {
								status = PCAN_USB.Init(PCAN_USB.CAN_BAUD_1M, PCAN_USB.CAN_INIT_TYPE_EX);
							} catch {
								String msgHelp = "There is another PCAN instance open. Please close first.";
								DialogResult dialog = MessageBox.Show(msgHelp);
							
								if (dialog == DialogResult.OK) {
									Application.Exit();
								}
								//return;
							}
						}
	                    /*---End Check PCAN---*/
	                    
	                    
						if (status == PCAN_USB.ERR_OK) {
							PCAN_USB.MsgFilter(0, PCAN_USB.CAN_MAX_EXTENDED_ID, PCAN_USB.MSGTYPE_EXTENDED);
							can_init = true;
						}
	                    else {
	                        can_init = false;
                        	can_init_delay = 1000;			// 1 second delay
	                    }

            		}
            		catch {
	                	can_init = false;
                    	can_init_delay = 5000;				// 5 second delay
            		}
                }
            	if(can_init == true) {
                    // check CAN status
                    uint status = PCAN_USB.Status();
                    if (status != PCAN_USB.ERR_OK) {
                        // shutdown 
                        can_init = false;
                        Thread.Sleep(200);
                        PCAN_USB.Close();
                    }
                    else {
                        // CAN is ok, let's empty the receive queue
                        PCAN_USB.TPCANMsg msg = new PCAN_USB.TPCANMsg();
                        while ((PCAN_USB.Read(out msg) & PCAN_USB.ERR_QRCVEMPTY) == 0) {
                            if (msg.MSGTYPE == PCAN_USB.MSGTYPE_EXTENDED) {
                                // extended message received
                                j1939msg jmsg = new j1939msg();
                                jmsg.prio = (byte)(msg.ID >> 24);
                                jmsg.pgn = (ushort)(msg.ID >> 8);
                                if ((jmsg.pgn >= 0xC000) && (jmsg.pgn < 0xF000))  {
                                    jmsg.pgn &= 0xFF00;
                                    jmsg.da = (byte)(msg.ID >> 8);
                                }
                                jmsg.sa = (byte)msg.ID;
                                jmsg.dlc = msg.LEN;
                                for (byte i = 0; i < jmsg.dlc; i++)  {
                                    jmsg.data[i] = msg.DATA[i];
                                }
                                // call the message handler
                                RxMsgHandler(jmsg);
                            }
                        }
                    }
                }
                Thread.Sleep(5);
            }
        }



    	private void RxMsgHandler(j1939msg rxmsg)
        {
            switch (rxmsg.pgn) {
                case 0xEC00:														// MCAN_PGN_TP_CM
    				if((rxmsg.da == PC_SA) || (rxmsg.da == 0xFF)) {              	// address for MFF transfers
    					if((rxmsg.data[0] == 0x10) || (rxmsg.data[0] == 0x20)) {    // received RTS
    						j1939tp rxTpMsg = FindRxTp(rxmsg.sa);					// use old Tp
    						if(rxTpMsg == null) rxTpMsg = FindRxTp(0);				// allocate new Tp
    						if(rxTpMsg != null) {
	                            rxTpMsg.exp_bytes = rxmsg.data[1];
	                            rxTpMsg.exp_packets = rxmsg.data[3];
	                            rxTpMsg.sa = rxmsg.sa;
	                            rxTpMsg.da = rxmsg.da;
	                            rxTpMsg.pgn = (uint)rxmsg.data[5] + (uint)rxmsg.data[6] * 0x100;
	                            SendCTS(rxmsg);             							// transmit CTS
    						}
                        }
                        if ((rxmsg.data[0] == 0x11) && (tx_j1939_tp.state == j1939tp.STATE_WAIT_CTS)) {
                            // we are expecting this CTS
                            // start sending data packets
                            tx_j1939_tp.state = j1939tp.STATE_TX_DATA;
                            tx_j1939_tp.timestamp = DateTime.Now.AddMilliseconds(-10);
                        }
                    }
                    // TP_CM
                    if (rxmsg.da == PC_SA)  {
                        if ((rxmsg.data[0] == 0x11) && (tx_j1939_tp.state == j1939tp.STATE_WAIT_CTS)) {
                            // we are expecting this CTS
                            // start sending data packets
                            tx_j1939_tp.state = j1939tp.STATE_TX_DATA;
                            tx_j1939_tp.timestamp = DateTime.Now.AddMilliseconds(-10);
                        }
                    }
                    break;
                case 0xEB00:                                 						// receive transfer protocol message
                    if((rxmsg.da == PC_SA) || (rxmsg.da == 0xFF)) {					// address for MFF transfers
    					j1939tp rxTpMsg = FindRxTp(rxmsg.sa);
    					if(rxTpMsg != null) {
	                        int seqNum = rxmsg.data[0];
	                        if((seqNum>=1) && (seqNum<=rxTpMsg.exp_packets)) {
	                            int Start = (seqNum-1) * 7;
	                            int cnt;
	                            for(cnt=1; cnt<8; cnt++) {
	                                rxTpMsg.MffData[Start +cnt -1] = rxmsg.data[cnt];
	                            }
	                        }
	                        if(seqNum==rxTpMsg.exp_packets) {						// end of transfer protocol
	                        	if(rxTpMsg.da == PC_SA) {
	                            	SendAckTp(rxTpMsg);								// send ack for transfer complete
	                        	}
	                            CanRxMsgSorter(rxTpMsg);
	                        }
    					}
                    }
                    break;
				case MCAN_PGN_DM15:
					gwProcessDM15(rxmsg);
					break;
				case 0xEE00:							// MCAN_PGN_ADDR_CLAIM
	               	if(SrcAddrList.Contains(rxmsg.sa) == false) {
                   		SrcAddrList.Add(rxmsg.sa);
                    }
                   	break;
                default:
                    break;
            }
        }



    	private void CanRxMsgSorter(j1939tp rxTpMsg)
    	{
		    /*! /note This function is recursive to 1 level...be careful!! */
		    // make sure this is for us
		    if ((rxTpMsg.da == MCAN_SA_GLOBAL) || (rxTpMsg.da == PC_SA)) {
				// route to the appropriate handler
				switch (rxTpMsg.pgn) {
					case MCAN_PGN_COMPONENT_ID:
						gwProcessCompID(rxTpMsg);
						break;
					default:
					    break;
				}
				/* pass the message to the application layer via callback function */
				if(rxTpMsg.sa == gw_response_mod) {
	                byte[] byteData = CopyToLength(rxTpMsg.MffData, rxTpMsg.exp_bytes);
	                string strData = Encoding.UTF8.GetString(byteData);    // convert byte[] to string
	                strMffReply = strData;
	                if(strData.StartsWith("H")) {
						MsgToProduct(rxTpMsg.sa.ToString("X2"), "PGN", strData);
	                }
				}
		    }
    	}


		// find j1939 transfer protocol message by source address.
		// source address 0 is unused.
    	private j1939tp FindRxTp(byte srcAddr)
    	{
    		foreach(j1939tp msg in j1939TpRxList) {
    			if(msg.sa == srcAddr) {
    				return(msg);
    			}
    		}
    		return(null);						// dstAddr not found
    	}
    	

    	void gwProcessCompID(j1939tp rxTpMsg)
		{
			if ((rxTpMsg.sa == gw_response_mod) && (gw_response == GW_RESP_COMP_ID)) {
				/* we were expecting this response */
				/* make sure it is properly terminated */
                byte[] byteData = CopyToLength(rxTpMsg.MffData, rxTpMsg.exp_bytes);
                string strData = Encoding.UTF8.GetString(byteData);    		// convert byte[] to string
                strMffReply = strData;
                string srcAddr = rxTpMsg.sa.ToString("X02");
				string strMsg = "HF9" + srcAddr + "0303;0;" + strData + ";00\n";
				MsgToProduct(rxTpMsg.sa.ToString("X2"), "PGN", strMsg);
                gw_response = GW_RESP_NONE;
			}
		}


    	void gwProcessDM15(j1939msg rxmsg)
    	{
			/* make sure we got a response from the correct module */
    		if(rxmsg.sa == gw_response_mod) {
				int status = (rxmsg.data[0] & 0x0E) >> 1;
				switch (gw_response) {
//					case GW_RESP_EXIT_TO_BOOT:
//						if (status == MEM_ST_OP_COMP) {
//							/* delay some time to allow the module to reboot */
//							gw_delay = 500;
//						}
//						break;
					case GW_RESP_ERASE:
						if (status == MEM_ST_OP_COMP) {
							/* now we need to write the memory */
							SendDM14 (gw_response_mod, gw_addr, 16, MEM_WRITE, MEM_SPACE_FLASH);
							gw_response = GW_RESP_WRITE;
							gw_tmr_limit = 5;				// 0.5 seconds

						} else {
							gw_response = GW_RESP_NONE;
						}
						break;
					case GW_RESP_WRITE:
						if (status == MEM_ST_PROCEED) {
							/* send the DM16 */
							j1939msg msg = new j1939msg();
							msg.prio = 0x00;
							msg.pgn = MCAN_PGN_DM16;
							msg.da = (byte)gw_response_mod;
							msg.sa = PC_SA;
							msg.dlc = 16;
							for(byte i=0; i<16; i++) {
								msg.data[i] = (byte)gw_buff[i];
							}
							TransmitJ1939(msg);
						} else if (status == MEM_ST_OP_COMP) {
							/* write operation is complete, send the response */
							gw_response = GW_RESP_NONE;
							MsgToProduct(rxmsg.sa.ToString("X2"), "PGN", "HF980;W;\n");
						} else {
							MsgToProduct(rxmsg.sa.ToString("X2"), "PGN", "\n");
							gw_response = GW_RESP_NONE;
						}
						break;
					case GW_RESP_CALC_CKS:
						if (status == MEM_ST_OP_COMP) {
							/* extract the checksum */
							GatewayCheckSum = rxmsg.data[1];
							GatewayCheckSum |= (int)(rxmsg.data[2]) << 8;
							string strMsg = "HF9" + gw_response_mod.ToString("X02") + "0305;" + GatewayCheckSum.ToString("X4") + ";00\n";
							MsgToProduct(rxmsg.sa.ToString("X2"), "PGN", strMsg);
						}
						break;
					case GW_RESP_CKS_WRITE:
						if (status == MEM_ST_PROCEED) {
							/* send DM16 */
							j1939msg msg = new j1939msg();
							msg.prio = 0x00;
							msg.pgn = MCAN_PGN_DM16;
							msg.da = (byte)gw_response_mod;
							msg.sa = PC_SA;
							msg.dlc = 2;
							msg.data[0] = (byte)(GatewayCheckSum & 0xff);
							msg.data[1] = (byte)(GatewayCheckSum >> 8);
							TransmitJ1939(msg);
						} else if (status == MEM_ST_OP_COMP) {
							MsgToProduct(rxmsg.sa.ToString("X2"), "PGN", "C\n");
							gw_response = GW_RESP_NONE;
						} else {
							MsgToProduct(rxmsg.sa.ToString("X2"), "PGN", "\n");
							gw_response = GW_RESP_NONE;
						}
						break;
					default:
						break;
				}
			}
    	}


    	private void SendCTS(j1939msg rxmsg)
        {
            j1939msg msg = new j1939msg();
            msg.prio = 0x18;
            msg.pgn = 0xEC00;
            msg.da = (byte)ModuleAddr;
            msg.sa = PC_SA;
            msg.dlc = 8;
            msg.data[0] = 0x11;
            msg.data[1] = rxmsg.data[3];
            msg.data[2] = 1;
            msg.data[3] = 0xff;
            msg.data[4] = 0xff;
            msg.data[5] = rxmsg.data[5];
            msg.data[6] = rxmsg.data[6];
            msg.data[7] = 0;
            TransmitJ1939(msg);
        }


        private void SendAckTp(j1939tp rxTpMsg)
        {
            j1939msg msg = new j1939msg();
            msg.prio = 0x18;
            msg.pgn = 0xEC00;
            msg.da = (byte)ModuleAddr;
            msg.sa = PC_SA;
            msg.dlc = 8;
            msg.data[0] = 0x13;
            msg.data[1] = (byte)rxTpMsg.exp_bytes;
            msg.data[2] = 0;
            msg.data[3] = rxTpMsg.exp_packets;
            msg.data[4] = 0xFF;
            msg.data[5] = 0;
            msg.data[6] = 0xEF;
            msg.data[7] = 0;
            TransmitJ1939(msg);
        }




    }	// end of class

}	// end of namespace

