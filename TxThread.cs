/*
 * Created by SharpDevelop.
 * User: dbw
 * Date: 7/20/2015
 * Time: 2:11 PM
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


        private void TxThread ()
        {
            tx_thread_exit = false;
            while (tx_thread_exit == false) {
                if ((tx_j1939_tp != null) && (tx_j1939_tp.state == j1939tp.STATE_TX_DATA))  {
                    if (DateTime.Now >= tx_j1939_tp.timestamp.AddMilliseconds(1))  {
                        // time to transmit next packet
                        j1939msg msg = new j1939msg();

                        msg.prio = 0x18;
                        msg.pgn = 0xEB00;
                        msg.da = tx_j1939_tp.da;
                        msg.sa = PC_SA;
                        msg.dlc = 8;
                        msg.data = new byte[8];
                        msg.data[0] = (byte)(tx_j1939_tp.packets + 1);
                        for (byte i = 0; (i < 7) && (tx_j1939_tp.bytes < tx_j1939_tp.exp_bytes); i++, tx_j1939_tp.bytes++)  {
                            msg.data[i + 1] = tx_j1939_tp.data[tx_j1939_tp.bytes];
                        }
                        TransmitJ1939(msg);
                        tx_j1939_tp.packets++;
                        if (tx_j1939_tp.packets == tx_j1939_tp.exp_packets) {
                            // done...
                            tx_j1939_tp.state = j1939tp.STATE_IDLE;
                        }
                        else  {
                            // more packets to transmit
                            tx_j1939_tp.timestamp = DateTime.Now;
                        }
                    }
                }
                else {
                    Thread.Sleep(5);
                }
            }
        }


        private void TransmitJ1939(j1939msg msg)
        {
            if (msg.dlc <= 8) {
                PCAN_USB.TPCANMsg canmsg = new PCAN_USB.TPCANMsg();

                // set message type
                canmsg.MSGTYPE = PCAN_USB.MSGTYPE_EXTENDED;

                // form identifier
                canmsg.ID = (uint)msg.prio << 24;
                canmsg.ID |= (uint)msg.pgn << 8;
                if ((msg.pgn >= 0xC000) && (msg.pgn < 0xF000))  {
                    canmsg.ID |= (uint)msg.da << 8;
                }
                canmsg.ID |= (uint)msg.sa;

                // set DLC and data
                canmsg.LEN = (byte)msg.dlc;

                canmsg.DATA = new byte[8];

                for (byte i = 0; i < msg.dlc; i++)  {
                    canmsg.DATA[i] = msg.data[i];
                }

                // write
                PCAN_USB.Write(ref canmsg);
            }
            else  {                       // must be multi-packet 
        		tx_j1939_tp.pgn = (ushort)msg.pgn;
                tx_j1939_tp.da = msg.da;
                tx_j1939_tp.exp_bytes = (ushort)msg.dlc;
                tx_j1939_tp.bytes = 0;
                tx_j1939_tp.exp_packets = (byte)(msg.dlc / 7);
                if ((msg.dlc % 7) > 0)  {
                    tx_j1939_tp.exp_packets++;
                }
                tx_j1939_tp.packets = 0;
                // copy the data
                tx_j1939_tp.data = new byte[256];
                for (ushort i = 0; i < msg.dlc; i++)  {
                    tx_j1939_tp.data[i] = msg.data[i];
                }
                
                // form the TPCM message
                j1939msg jmsg = new j1939msg();
                jmsg.prio = 0x18;
                jmsg.pgn = 0xEC00;
                jmsg.da = tx_j1939_tp.da;
                jmsg.sa = PC_SA;
                jmsg.dlc = 8;
                jmsg.data = new byte[8];
                jmsg.data[1] = (byte)tx_j1939_tp.exp_bytes;
                jmsg.data[2] = (byte)(tx_j1939_tp.exp_bytes >> 8);
                jmsg.data[3] = tx_j1939_tp.exp_packets;
                jmsg.data[4] = 0xff;
                jmsg.data[5] = (byte)tx_j1939_tp.pgn;
                jmsg.data[6] = (byte)(tx_j1939_tp.pgn >> 8);
                jmsg.data[7] = 0x00;
                if (jmsg.da == 0xff) {                     // send BAM
                    jmsg.data[0] = 0x20;
                    tx_j1939_tp.state = j1939tp.STATE_TX_DATA;
                }
                else {                                     // send RTS
                    jmsg.data[0] = 0x10;
                    tx_j1939_tp.state = j1939tp.STATE_WAIT_CTS;
                }
                tx_j1939_tp.timestamp = DateTime.Now;
                TransmitJ1939(jmsg);
            }
        }



    }	// end of class

}	// end of namespace

