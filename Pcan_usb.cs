///////////////////////////////////////////////////////////////////////////////
//	PCAN-Light
//  PCAN_USB.cs
//
//  Version 2.x
//
//  ~~~~~~~~~~
//
//  Definition of the PCAN-Light API. 
//	The Driver support a Hardware and a Software who want to communicate with CAN-busses 
//
//  ~~~~~~~~~~~~
//
//  PCAN-Light-API
//
//  ~~~~~~~~~~~~
//
//	- Init(ushort wBTR0BTR1, int Type)		
//	- Close()  
//	- Status() 
//	- Write(ref TPCANMsg msg) 
//	- Read(out TPCANMsg msg)  
//	- ReadEx(out TPCANMsg msg, out TPCANTimestamp timestamp)  
//	- VersionInfo(StringBuilder buffer) 
//  - DLLVersionInfo(StringBuilder buffer)
//  - SpecialFunktion(uint distributorcode, uint codenumber)
//	- ResetClient() 
//	- MsgFilter(uint FromID, uint ToID, int Type) 
//	- ResetFilter() 
//	- SetUSBDeviceNr(uint uDevNo) 
//	- GetUSBDeviceNr(out uint DevNum) 
//  - SetRcvEvent(IntPtr hEvent)
//
//  ------------------------------------------------------------------
//  Author : Hoppe, Wilhelm
//  Modified By: Wagner (28.09.2009)
//
//  Language: C# 1.0
//  ------------------------------------------------------------------
//
//  Copyright (C) 1999-2009  PEAK-System Technik GmbH, Darmstadt
//
using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Peak.Can.Light
{
    public class PCAN_USB
    {
        #region Frames, ID's and CAN message types
        // Constants definitions - Frame Type
        public const int CAN_INIT_TYPE_EX = 1;     // Extended Frames
        public const int CAN_INIT_TYPE_ST = 0;     // Standard Frames

        // Constants definitions - ID
        public const int CAN_MAX_STANDARD_ID = 0x7ff;
        public const int CAN_MAX_EXTENDED_ID = 0x1fffffff;

        // Constants definitions  - CAN message types
        public const int MSGTYPE_STANDARD = 0x00;  // Standard Data frame (11-bit ID)
        public const int MSGTYPE_RTR = 0x01;  // 1, if Remote Request frame
        public const int MSGTYPE_EXTENDED = 0x02;  // 1, if Extended Data frame (CAN 2.0B, 29-bit ID)
        public const int MSGTYPE_STATUS = 0x80;  // 1, if Status information
        #endregion

        #region  Baurate Codes
        // Baud rate codes = BTR0/BTR1 register values for the CAN controller 
        public const int CAN_BAUD_1M = 0x0014;     //   1 MBit/sec
        public const int CAN_BAUD_500K = 0x001C;   // 500 KBit/sec
        public const int CAN_BAUD_250K = 0x011C;   // 250 KBit/sec
        public const int CAN_BAUD_125K = 0x031C;   // 125 KBit/sec
        public const int CAN_BAUD_100K = 0x432F;   // 100 KBit/sec
        public const int CAN_BAUD_50K = 0x472F;    //  50 KBit/sec
        public const int CAN_BAUD_20K = 0x532F;    //  20 KBit/sec
        public const int CAN_BAUD_10K = 0x672F;    //  10 KBit/sec
        public const int CAN_BAUD_5K = 0x7F7F;     //   5 KBit/sec
        // you can define your own Baudrate with the BTROBTR1 register !!
        // take a look at www.peak-system.com for our software BAUDTOOL to
        // calculate the BTROBTR1 register for every baudrate and sample point.
        #endregion

        #region Error Codes
        public const int ERR_OK = 0x0000;  // No error
        public const int ERR_XMTFULL = 0x0001;  // Transmit buffer in CAN controller is full
        public const int ERR_OVERRUN = 0x0002;  // CAN controller was read too late
        public const int ERR_BUSLIGHT = 0x0004;  // Bus error: an error counter reached the 'light' limit
        public const int ERR_BUSHEAVY = 0x0008;  // Bus error: an error counter reached the 'heavy' limit  
        public const int ERR_BUSOFF = 0x0010;  // Bus error: the CAN controller is in bus-off state
        public const int ERR_QRCVEMPTY = 0x0020;  // Receive queue is empty
        public const int ERR_QOVERRUN = 0x0040;  // Receive queue was read too late
        public const int ERR_QXMTFULL = 0x0080;  // Transmit queue ist full
        public const int ERR_REGTEST = 0x0100;  // Test of the CAN controller hardware registers failed (no hardware found)
        public const int ERR_NOVXD = 0x0200;  // Driver not loaded
        public const int ERR_NODRIVER = 0x0200;  // Driver not loaded
        public const int ERRMASK_ILLHANDLE = 0x1C00;  // Mask for all handle errors
        public const int ERR_HWINUSE = 0x0400;  // Hardware already in use by a Net
        public const int ERR_NETINUSE = 0x0800;  // a Client is already connected to the Net
        public const int ERR_ILLHW = 0x1400;  // Hardware handle is invalid
        public const int ERR_ILLNET = 0x1800;  // Net handle is invalid
        public const int ERR_ILLCLIENT = 0x1C00;  // Client handle is invalid
        public const int ERR_RESOURCE = 0x2000;  // Resource (FIFO, Client, timeout) cannot be created
        public const int ERR_ILLPARAMTYPE = 0x4000;  // Invalid parameter
        public const int ERR_ILLPARAMVAL = 0x8000;  // Invalid parameter value
        public const int ERR_UNKNOWN = 0x10000; // Unknown error
        public const int ERR_ANYBUSERR = ERR_BUSLIGHT | ERR_BUSHEAVY | ERR_BUSOFF;
        #endregion


        #region Structures
        // CAN message
        //
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TPCANMsg
        {
            public uint ID;           // 11/29 bit identifier
            public byte MSGTYPE;      // Bits from MSGTYPE_*
            public byte LEN;          // Data Length Code of the Msg (0..8)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] DATA;       // Data 0 .. 7		
        }

        // Timestamp of a receive/transmit event
        // Total microseconds = micros + 1000 * millis + 0xFFFFFFFF * 1000 * millis_overflow
        //
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TPCANTimestamp
        {
            public uint millis;             // Base-value: milliseconds: 0.. 2^32-1
            public ushort millis_overflow;  // Roll-arounds of millis
            public ushort micros;           // Microseconds: 0..999
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////
        //  Init()
        //  This function make the following:
        //		- Activate a Hardware
        //		- Make a Register Test of 82C200/SJA1000
        //		- Allocate a Send buffer and a Hardware handle
        //		- Programs the configuration of the transmit/receive driver
        //		- Set the Baudrate register
        //		- Set the Controller in RESET condition	
        //		
        //  If CANMsgType=0  ---> ID 11Bit
        //  If CANMsgType=1  ---> ID 11/29Bit 
        //
        //  Possible Errors: NOVXD ILLHW REGTEST RESOURCE
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_Init")]
        public static extern uint Init(ushort BTR0BTR1, int CANMsgType);

        ///////////////////////////////////////////////////////////////////////////////
        //  Close()
        //  This function terminate and release the configured hardware and all 
        //  allocated resources
        //
        //  Possible Errors: NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_Close")]
        public static extern uint Close();

        ///////////////////////////////////////////////////////////////////////////////
        //  Status()
        //  This function request the current status of the hardware (b.e. BUS-OFF)
        //
        //  Possible Errors: NOVXD BUSOFF BUSHEAVY OVERRUN
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_Status")]
        public static extern uint Status();

        ///////////////////////////////////////////////////////////////////////////////
        //  Write()
        //  This function Place a CAN message into the Transmit Queue of the CAN Hardware
        //
        //  Possible Errors: NOVXD RESOURCE BUSOFF QXMTFULL
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_Write")]
        public static extern uint Write(ref TPCANMsg msg);

        ///////////////////////////////////////////////////////////////////////////////
        //  Read()
        //  This function get the next message or the next error from the Receive Queue of 
        //  the CAN Hardware.  
        //  REMARK:
        //		- Check always the type of the received Message (MSGTYPE_STANDARD,MSGTYPE_RTR,
        //		  MSGTYPE_EXTENDED,MSGTYPE_STATUS)
        //		- The function will return ERR_OK always that you receive a CAN message successfully 
        //		  although if the messages is a MSGTYPE_STATUS message.  
        //		- When a MSGTYPE_STATUS mesasge is got, the ID and Length information of the message 
        //		  will be treated as indefined values. Actually information of the received message
        //		  should be interpreted using the first 4 data bytes as follow:
        //			*	Data0	Data1	Data2	Data3	Kind of Error
        //				0x00	0x00	0x00	0x02	CAN_ERR_OVERRUN		0x0002	CAN Controller was read to late
        //				0x00	0x00	0x00	0x04	CAN_ERR_BUSLIGHT	0x0004  Bus Error: An error counter limit reached (96)
        //				0x00	0x00	0x00	0x08	CAN_ERR_BUSHEAVY	0x0008	Bus Error: An error counter limit reached (128)
        //				0x00	0x00	0x00	0x10	CAN_ERR_BUSOFF		0x0010	Bus Error: Can Controller went "Bus-Off"
        //		- If a CAN_ERR_BUSOFF status message is received, the CAN Controller must to be 
        //		  initialized again using the Init() function.  Otherwise, will be not possible 
        //		  to send/receive more messages. 
        //		- The message will be written to 'msgbuff'.
        //
        //  Possible Errors: NOVXD  QRCVEMPTY
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_Read")]
        public static extern uint Read(out TPCANMsg msg);

        ///////////////////////////////////////////////////////////////////////////////
        //  ReadEx()
        //  This function get the next message or the next error from the Receive Queue of 
        //  the CAN Hardware and the time when the message arrived. 
        //  REMARK:
        //		- Check always the type of the received Message (MSGTYPE_STANDARD,MSGTYPE_RTR,
        //		  MSGTYPE_EXTENDED,MSGTYPE_STATUS)
        //		- The function will return ERR_OK always that you receive a CAN message successfully 
        //		  although if the messages is a MSGTYPE_STATUS message.  
        //		- When a MSGTYPE_STATUS mesasge is got, the ID and Length information of the message 
        //		  will be treated as indefined values. Actually information of the received message
        //		  should be interpreted using the first 4 data bytes as follow:
        //			*	Data0	Data1	Data2	Data3	Kind of Error
        //				0x00	0x00	0x00	0x02	CAN_ERR_OVERRUN		0x0002	CAN Controller was read to late
        //				0x00	0x00	0x00	0x04	CAN_ERR_BUSLIGHT	0x0004  Bus Error: An error counter limit reached (96)
        //				0x00	0x00	0x00	0x08	CAN_ERR_BUSHEAVY	0x0008	Bus Error: An error counter limit reached (128)
        //				0x00	0x00	0x00	0x10	CAN_ERR_BUSOFF		0x0010	Bus Error: Can Controller went "Bus-Off"
        //		- If a CAN_ERR_BUSOFF status message is received, the CAN Controller must to be 
        //		  initialized again using the Init() function.  Otherwise, will be not possible 
        //		  to send/receive more messages. 
        //		- The message will be written to 'msgbuff'.
        //		Since Version 2.x the Ext. Version is available - new Parameter:
        //		-  Receive timestamp
        //
        //  Possible Errors: NOVXD  QRCVEMPTY
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_ReadEx")]
        public static extern uint ReadEx(out TPCANMsg msg, out TPCANTimestamp timestamp);

        ///////////////////////////////////////////////////////////////////////////////
        //  VersionInfo()
        //  This function get the Version and copyright of the hardware as text 
        //  (max. 255 characters)
        //
        //  Possible Errors:  NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_VersionInfo")]
        public static extern uint VersionInfo(StringBuilder buffer);

        ///////////////////////////////////////////////////////////////////////////////
        //  DLLVersionInfo()
        //  This function is used to get the Version and copyright of the 
        //  DLL as text (max. 255 characters)
        //
        //  Possible Errors: -1 for NULL-Pointer parameters :-)
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_DLLVersionInfo")]
        public static extern uint DLLVersionInfo(StringBuilder buffer);

        ///////////////////////////////////////////////////////////////////////////////
        //  SpecialFunktion()
        //  This function is an special function to be used "ONLY" for distributors
        //  Return: 1 - the given parameters and the parameters in the hardware agree 
        //		    0 - otherwise
        //
        //  Possible Errors:  NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_SpecialFunktion")]
        public static extern uint SpecialFunktion(uint distributorcode, uint codenumber);

        //////////////////////////////////////////////////////////////////////////////
        //  ResetClient()
        //  This function delete the both queues (Transmit,Receive) of the CAN Controller 
        //  using a RESET
        //
        //  Possible Errors: ERR_ILLCLIENT ERR_NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_ResetClient")]
        public static extern uint ResetClient();

        ///////////////////////////////////////////////////////////////////////////////
        //  MsgFilter(FromID, ToID, int Type)
        //  This function set the receive message filter of the CAN Controller.
        //  REMARK:
        //		- A quick register of all messages is possible using the parameters FromID and ToID = 0
        //		- Every call of this function maybe cause an extention of the receive filter of the 
        //		  CAN controller, which one can go briefly to RESET
        //		- New in Ver 2.x:
        //			* Standard frames will be put it down in the acc_mask/code as Bits 28..13
        //			* Hardware driver for 82C200 must to be moved to Bits 10..0 again!
        //	WARNING: 
        //		It is not guaranteed to receive ONLY the registered messages.
        //
        //  Possible Errors: NOVXD ILLCLIENT ILLNET REGTEST
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_MsgFilter")]
        public static extern uint MsgFilter(uint FromID, uint ToID, int Type);

        ///////////////////////////////////////////////////////////////////////////////
        //  ResetFilter()
        //  This function close completely the Message Filter of the Hardware.
        //  They will be no more messages received.
        //
        //  Possible Errors: NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_ResetFilter")]
        public static extern uint ResetFilter();

        //////////////////////////////////////////////////////////////////////////////
        //  SetUSBDeviceNr()
        //  This function set an identification number to the USB CAN hardware
        //
        //  Possible Errors: NOVXD ILLHW ILLPARAMTYPE ILLPARAMVAL REGTEST
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "SetUSBDeviceNr")]
        public static extern uint SetUSBDeviceNr(uint DevNum);

        //////////////////////////////////////////////////////////////////////////////
        //  GetUSBDeviceNr()
        //  This function read the device number of a USB CAN Hardware
        //
        //  Possible Errors: NOVXD ILLHW ILLPARAMTYPE
        //	
        [DllImport("PCAN_USB.dll", EntryPoint = "GetUSBDeviceNr")]
        public static extern uint GetUSBDeviceNr(out uint DevNum);

        ///////////////////////////////////////////////////////////////////////////////
        //  SetRcvEvent()
        //  This function is used to set the Event for the Event Handler
        //
        //  Possible Errors: ILLCLIENT ILLPARAMTYPE ILLPARAMVAL NOVXD
        //
        [DllImport("PCAN_USB.dll", EntryPoint = "CAN_SetRcvEvent")]
        public static extern uint SetRcvEvent(IntPtr hEvent);
    }
}