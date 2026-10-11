using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JModbus
{
    public class Modbus 
    {
        private int _port;
        private IPAddress _ipAddress;
        private byte _slaveId;
        private byte[] _tcpRxBuf;
        private byte[] _tcpTxBuf;
        private byte[] _fbCoilBuf;
        private byte[] _fbInpCoilBuf;
        private ushort[] _fbInDataBuf;
        private const ushort MaxBits = 256;
        private const ushort MaxRegs = 1023;
        private const ushort MaxInt = 1023;
        private const ushort MaxPresetMultipleRegister = 64;
        private ushort addr_high = 8;
        private ushort addr_low = 9;
        private ushort quantity_high = 10;
        private ushort quantity_low = 11;
        private ushort byte_count = 12;
        private ushort data_high = 13;
        private ushort data_low = 14;
        private ushort temp0 = 0;
        private ushort temp1 = 0;
        private ushort temp2 = 0;
        private ushort tempNo = 0;
        private ushort bytLo = 0;
        private ushort bytHi = 0;
        private ushort bitPos = 0;
        private ushort modBytePos = 0;
        private byte srcBit = 0;
        private byte destBit = 0;
        private byte _tcpChrCnt;
        private bool tcp_send_data_flag;
        private bool _isDataChngd = false;

        private int _transmitCount;
        private int _receiveCount;
        private int _resetableErrorCount;
        private int _accumulatedErrorCount;
        private int _writeCount;

        private ushort tx_data_index = 9;
        private Thread connectionThread;
        TcpListener slaveTcpListener;

        public int TransmitCount()
        {
            return _transmitCount;
        }
        public int ReceiveCount()
        {
            return _receiveCount;
        }
        public int ResetableErrorCount()
        {
            return _resetableErrorCount;
        }
        public int AccumulatedErrorCount()
        {
            return _accumulatedErrorCount;
        }
        public int WriteCount()
        {
            return _writeCount;
        }

        /// <summary>
        /// Modbus Operations
        /// </summary>
        public enum ModbusOperation
        {
            ReadCoils = 1,
            ReadDiscreteInputs = 2,
            ReadMultipleHoldingRegisters = 3,
            ReadInputRegisters = 4,
            WriteSingleCoil = 5,
            WriteSingleHoldingRegister = 6,
            ReadExceptionStatus = 7, //serial only Diagnostics
            Diagnostic = 8, //serial only Diagnostics
            GetComEventCounter = 11, //serial only Diagnostics
            GetComEventLog = 12, //serial only Diagnostics
            WriteMultipleCoils = 15,
            WriteMultipleHoldingRegisters = 16,
            ReportServerID = 17,  //serial only Diagnostics
            ReadFileRecord = 20, // File Record Access	
            WriteFileRecord = 21, //File Record Access	
            MaskWriteRegister = 22,
            ReadWriteMultipleRegisters = 23,
            ReadFIFOQueue = 24,
            ReadDeviceIdentification = 43, // Diagnostics
            EncapsulatedInterfaceTransport = 43 //Other
        }
        /// <summary>
        /// Modbus Exception Codes
        /// </summary>
        public enum ModbusExceptionCodes
        {
            IllegalFunction = 1,//Function code received in the query is not recognized or allowed by server
            IllegalDataAddress = 2,//Data address of some or all the required entities are not allowed or do not exist in server
            IllegalDataValue = 3, //Value is not accepted by server
            ServerDeviceFailure = 4, //Unrecoverable error occurred while server was attempting to perform requested action
            Acknowledge = 5,//Server has accepted request and is processing it, but a long duration of time is required. This response is returned to prevent a timeout error from occurring in the client. client can next issue a Poll Program Complete message to determine whether processing is completed
            ServerDeviceBusy = 6, //Server is engaged in processing a long-duration command. client should retry later
            NegativeAcknowledge = 7, //Server cannot perform the programming functions. Client should request diagnostic or error information from server
            MemoryParityError = 8, //	Server detected a parity error in memory. Client can retry the request, but service may be required on the server device
            GatewayPathUnavailable = 10, //Specialized for Modbus gateways. Indicates a misconfigured gateway
            GatewayTargetDeviceFailedToRespond = 11
        }

        /// <summary>
        /// Mosbus Constructer
        /// </summary>
        /// <param name="iPAddress"></param>
        /// <param name="port"></param>
        public Modbus(IPAddress iPAddress, int port = 502)
        {
            _port = port;
            _ipAddress = iPAddress;
            _slaveId = 1;//default slave Id if not specified
            _tcpRxBuf = new byte[256];
            _tcpTxBuf = new byte[2048];
            _fbCoilBuf = new byte[256];
            _fbInpCoilBuf = new byte[256];
            _fbInDataBuf = new ushort[2048];
            _transmitCount = 0;
            _receiveCount = 0;
            _resetableErrorCount = 0;
            _accumulatedErrorCount = 0;
            _writeCount = 0;
            try
            {
                slaveTcpListener = new TcpListener(_ipAddress, _port);
                slaveTcpListener.Start();
                connectionThread = new Thread(new ThreadStart(StartModbus));
                connectionThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void StartModbus()
        {
            for (; ; )
            {
                try
                {
                    Byte[] bytesFrom = new Byte[2048];
                    Byte[] bytesTo = new Byte[2048];
                    int i;
                    NetworkStream networkStream;
                    TcpClient client;
                    bool _queryFlag = false;
                    while (true)
                    {
                        client = slaveTcpListener.AcceptTcpClient();
                        try
                        {
                            using (client)
                            using (networkStream = client.GetStream())
                            {
                                while ((i = networkStream.Read(bytesFrom, 0, bytesFrom.Length)) != 0)
                                {
                                    if (i >= 12)
                                    {
                                        _receiveCount++;
                                        if (_queryFlag == false)
                                        {
                                            _tcpRxBuf = bytesFrom;
                                            _queryFlag = true;
                                            if (DecodeModbusTCP() == 0)
                                            {
                                                if (tcp_send_data_flag)
                                                {
                                                    if (_resetableErrorCount > 0)
                                                        _resetableErrorCount--;
                                                    _transmitCount++;
                                                    networkStream.Write(_tcpTxBuf, 0, _tcpChrCnt);
                                                    networkStream.WriteTimeout = 500;
                                                }
                                            }
                                            else
                                            {
                                                _accumulatedErrorCount++;
                                                _resetableErrorCount++;
                                            }
                                            _queryFlag = false;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception clientEx)
                        {
                            // Client disconnected or closed socket
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Server error: {0}", ex.Message);
                }
                //finally
                //{
                //    //slaveTcpListener.Stop();
                //}
            }
        }
        /// <summary>
        /// Set Input Data in Integer, will accumulate 2 registers
        /// Example If Start address is 4 then 4 and 5 will be filled
        /// </summary>
        /// <param name="startAddress">Specify Start Address (0 to 2047)</param>
        /// <param name="data">Any Integer Value</param>
        /// <returns>On Error will Retun Error Code otheriwse 0</returns>
        public ushort SetInputRegister(ushort startAddress, int data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            ushort upper = (ushort)(data >> 16);
            ushort lower = (ushort)(data & 0xffff);
            _fbInDataBuf[startAddress] = lower;
            _fbInDataBuf[startAddress+1] = upper;
            return 0;
        }
        /// <summary>
        /// Set Input Data in Long, will accumulate 4 registers
        /// Example If Start address is 4 then 4,5,6 and 7 will be filled
        /// </summary>
        /// <param name="startAddress">Specify Start Address (0 to 2047)</param>
        /// <param name="data">Any Long Value</param>
        /// <returns>On Error will Retun Error Code otheriwse 0</returns>
        public ushort SetInputRegister(ushort startAddress, long data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            byte[] inputData = BitConverter.GetBytes(data);
            try
            {
                _fbInDataBuf[startAddress] = (ushort)(inputData[0] + (inputData[1] << 8));
                _fbInDataBuf[startAddress + 1] = (ushort)(inputData[2] + (inputData[3] << 8));
                _fbInDataBuf[startAddress + 2] = (ushort)(inputData[4] + (inputData[5] << 8));
                _fbInDataBuf[startAddress + 3] = (ushort)(inputData[6] + (inputData[7] << 8));
            }
            catch
            {
                return (ushort)ModbusExceptionCodes.IllegalDataValue;
            }
            return 0;
        }
        /// <summary>
        /// Set Input Data in Float, will accumulate 2 registers
        /// Example If Start address is 4 then 4 and 5 will be filled
        /// </summary>
        /// <param name="startAddress">Specify Start Address (0 to 2047)</param>
        /// <param name="data">Any Float Value</param>
        /// <returns>On Error will Retun Error Code otheriwse 0</returns>
        public ushort SetInputRegister(ushort startAddress, float data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            byte[] inputData = BitConverter.GetBytes(data);
            try
            {
                _fbInDataBuf[startAddress] = (ushort)(inputData[0] + (inputData[1] << 8));
                _fbInDataBuf[startAddress + 1] = (ushort)(inputData[2] + (inputData[3] << 8));
            }
            catch
            {
                return (ushort)ModbusExceptionCodes.IllegalDataValue;
            }
            return 0;
        }
        /// <summary>
        /// Set Input Data in Double, will accumulate 4 registers
        /// Example If Start address is 4 then 4,5,6 and 7 will be filled
        /// </summary>
        /// <param name="startAddress">Specify Start Address (0 to 2047)</param>
        /// <param name="data">Any Double Value</param>
        /// <returns>On Error will Retun Error Code otheriwse 0</returns>
        public ushort SetInputRegister(ushort startAddress, double data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            byte[] inputData = BitConverter.GetBytes(data);
            try
            {
                _fbInDataBuf[startAddress] = (ushort)(inputData[0] + (inputData[1] << 8));
                _fbInDataBuf[startAddress + 1] = (ushort)(inputData[2] + (inputData[3] << 8));
                _fbInDataBuf[startAddress + 2] = (ushort)(inputData[4] + (inputData[5] << 8));
                _fbInDataBuf[startAddress + 3] = (ushort)(inputData[6] + (inputData[7] << 8));
            }
            catch
            {
                return (ushort)ModbusExceptionCodes.IllegalDataValue;
            }
            return 0;
        }
        /// <summary>
        /// Set Input Data in Unsigned Short, will accumulate 1 register
        /// </summary>
        /// <param name="startAddress">Specify Start Address (0 to 2047)</param>
        /// <param name="data">Any Unsigned Short data</param>
        /// <returns>On Error will Retun Error Code otheriwse 0</returns>
        public ushort SetInputRegister(ushort startAddress, ushort data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            _fbInDataBuf[startAddress] = data;
            return 0;
        }
        /// <summary>
        /// Set Input Data in String, will accumulate half of the length of string registers
        /// If string length is 4 then 2 registes will be filled 
        /// in case of length is 5 then 3 registers will be filled last register will hold space (0x20)
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ushort SetInputRegister(ushort startAddress, string data)
        {
            if (startAddress > (ushort)2048)
            {
                return (ushort)ModbusExceptionCodes.IllegalDataAddress;
            }
            int length = data.Length;
            if (length % 2 != 0)
            {
                data += " ";
                length++;
            }
            int i, j = 0;
            try
            {
                for (i = 0; i < (length/2); i++)
                {
                    _fbInDataBuf[startAddress + i] = (ushort)(data[j] << 8 | data[j + 1]);
                    j += 2;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (ushort)ModbusExceptionCodes.IllegalDataValue;
            }
            return 0;
        }
        public int ReadIntInputRegister(ushort startAddress)
        {
            return _fbInDataBuf[startAddress] << 16 | _fbInDataBuf[startAddress + 1];
        }
        public ushort ReadUShortInputRegister(ushort startAddress)
        {
            return _fbInDataBuf[startAddress];
        }
        public long ReadLongInputRegister(ushort startAddress)
        {
            byte[] target = new byte[8];
            target[0] = (byte)_fbInDataBuf[startAddress];
            target[1] = (byte)(_fbInDataBuf[startAddress] >> 8);
            target[2] = (byte)_fbInDataBuf[startAddress+1];
            target[3] = (byte)(_fbInDataBuf[startAddress+1] >> 8);
            target[4] = (byte)_fbInDataBuf[startAddress+2];
            target[5] = (byte)(_fbInDataBuf[startAddress+2] >> 8);
            target[6] = (byte)_fbInDataBuf[startAddress+3];
            target[7] = (byte)(_fbInDataBuf[startAddress+3] >> 8);
            return BitConverter.ToInt64(target,0); 
        }
        public float ReadFloatInputRegister(ushort startAddress)
        {
            byte[] target = new byte[4];
            target[0] = (byte)_fbInDataBuf[startAddress];
            target[1] = (byte)(_fbInDataBuf[startAddress] >> 8);
            target[2] = (byte)_fbInDataBuf[startAddress + 1];
            target[3] = (byte)(_fbInDataBuf[startAddress + 1] >> 8);
            target[4] = (byte)_fbInDataBuf[startAddress + 2];
            return (float)BitConverter.ToDouble(target,0);
        }
        public double ReadDoubleInputRegister(ushort startAddress)
        {
            byte[] target = new byte[8];
            target[0] = (byte)_fbInDataBuf[startAddress];
            target[1] = (byte)(_fbInDataBuf[startAddress] >> 8);
            target[2] = (byte)_fbInDataBuf[startAddress + 1];
            target[3] = (byte)(_fbInDataBuf[startAddress + 1] >> 8);
            target[4] = (byte)_fbInDataBuf[startAddress + 2];
            target[5] = (byte)(_fbInDataBuf[startAddress + 2] >> 8);
            target[6] = (byte)_fbInDataBuf[startAddress + 3];
            target[7] = (byte)(_fbInDataBuf[startAddress + 3] >> 8);
            return BitConverter.ToDouble(target, 0);
        }
        public string ReadInputRegister(ushort startAddress, ushort length)
        {
            StringBuilder sb = new StringBuilder();
            if (length % 2 != 0)
            {
                length++;
            }
            int i;
            byte myChar;
            for (i = 0; i < (length / 2); i++)
            {
                myChar = (byte)(_fbInDataBuf[startAddress + i] >> 8);
                sb.Append((char)myChar);
                myChar = (byte)_fbInDataBuf[startAddress + i];
                sb.Append((char)myChar);
            }
            return sb.ToString().Trim();
        }
        /// <summary>
        /// Set Slave Id if not provided then It will consider it as 1 (0x01)
        /// </summary>
        /// <param name="slaveId">Slave Id for the Identification</param>
        public void SlaveId(byte slaveId)
        {
            _slaveId = slaveId;
        }
        ushort BitValue(ushort b, int pos)
        {
            return (ushort)(b & (1 << pos));
        }
        private ushort DecodeModbusTCP()
        {
            ushort startRegister;
            ushort procRegister;
            ushort reqRegister;
            switch (_tcpRxBuf[7])
            {
                case (byte)ModbusOperation.ReadCoils:
                    reqRegister = (ushort)((_tcpRxBuf[10]*0x100)+ _tcpRxBuf[11]);
                    startRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));
                    procRegister = (ushort)(MaxBits - startRegister);
                    if (startRegister > MaxBits)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > procRegister)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > MaxBits)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    for (temp0 = 0; temp0 < reqRegister; temp0++)
                    {
                        temp1 = (ushort)(temp0 + startRegister);

                        /* Query */
                        bitPos = (ushort)(temp1 % 8);
                        bitPos = BitValue(bitPos,1);
                        modBytePos = (ushort)(temp1 / 8);

                        temp2 = (ushort)(_fbCoilBuf[modBytePos] & bitPos);

                        if (temp2 == 0)
                        {
                            srcBit = 0;
                        }
                        else
                        {
                            srcBit = 0xff;
                        }

                        temp1 = temp0;

                        /* Response */
                        modBytePos = (ushort)(temp1 / 8);
                        bitPos = (ushort)(temp1 % 8);
                        bitPos = BitValue(bitPos,1);

                        if (srcBit == 0xff)
                        {
                            _tcpTxBuf[tx_data_index + modBytePos] = (byte)(_tcpTxBuf[tx_data_index + modBytePos] | bitPos);
                        }
                        else
                        {
                            _tcpTxBuf[tx_data_index + modBytePos] = (byte)((_tcpTxBuf[tx_data_index + modBytePos]) & (bitPos ^ 0xff));
                        }
                    }
                    TCPCase1(reqRegister);
                    break;
                case (byte)ModbusOperation.ReadDiscreteInputs:
                    reqRegister = (ushort)((_tcpRxBuf[10] * 0x100) + _tcpRxBuf[11]);
                    startRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));
                    procRegister = (ushort)(MaxBits - startRegister);
                    _fbInpCoilBuf[0] = 0xff;
                    if (startRegister > MaxBits)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > procRegister)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > MaxBits)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    for (temp0 = 0; temp0 < reqRegister; temp0++)
                    {
                        temp1 = (ushort)(temp0 + startRegister);

                        /* Query */
                        bitPos = (ushort)(temp1 % 8);
                        bitPos = BitValue(bitPos,1);
                        modBytePos = (ushort)(temp1 / 8);

                        temp2 = (ushort)(_fbInpCoilBuf[modBytePos] & bitPos);

                        if (temp2 == 0)
                        {
                            srcBit = 0;
                        }
                        else
                        {
                            srcBit = 0xff;
                        }
                        temp1 = temp0;

                        /* Response */
                        modBytePos = (ushort)(temp1 / 8);
                        bitPos = (ushort)(temp1 % 8);
                        bitPos = BitValue(bitPos,1);

                        if (srcBit == 0xff)
                        {
                            _tcpTxBuf[tx_data_index + modBytePos] = (byte)(_tcpTxBuf[tx_data_index + modBytePos] | bitPos);
                        }
                        else
                        {
                            _tcpTxBuf[tx_data_index + modBytePos] = (byte)((_tcpTxBuf[tx_data_index + modBytePos]) & (bitPos ^ 0xff));
                        }
                    }
                    TCPCase2(reqRegister);
                    break;
                case (byte)ModbusOperation.ReadMultipleHoldingRegisters:
                    reqRegister = (ushort)((_tcpRxBuf[10] * 0x100) + _tcpRxBuf[11]);
                    startRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));
                    procRegister = (ushort)(MaxRegs - startRegister);
                    if (startRegister > MaxRegs)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    if (reqRegister > procRegister)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    if (reqRegister > MaxRegs)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress; 

                    TCPCase3(startRegister, reqRegister);
                    break;
                case (byte)ModbusOperation.ReadInputRegisters:
                    reqRegister = (ushort)((_tcpRxBuf[10] * 0x100) + _tcpRxBuf[11]);
                    startRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));
                    procRegister = (ushort)(MaxRegs - startRegister);

                    if (startRegister > MaxRegs)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    if (reqRegister > procRegister)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    if (reqRegister > MaxRegs)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;

                    TCPCase4(startRegister, reqRegister);
                    break;
                case (byte)ModbusOperation.WriteSingleCoil:
                    reqRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));

                    if (reqRegister > MaxBits)
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;

                    temp1 = reqRegister;
                    modBytePos = (ushort)(temp1 / 8);
                    bitPos = (ushort)(temp1 % 8);
                    bitPos = BitValue(bitPos,1);

                    /* force coil to off */
                    if (_tcpRxBuf[10] == 0)
                    {
                        destBit = (byte)(_fbCoilBuf[modBytePos] & ((bitPos) ^ 0xff));
                    }
                    else
                    { /* force coil to on */
                        destBit = (byte)(_fbCoilBuf[modBytePos] | bitPos);
                    }

                    if (destBit != _fbCoilBuf[modBytePos])
                    {
                        _fbCoilBuf[modBytePos] = destBit;
                        _isDataChngd = true;
                    }

                    if (_isDataChngd)
                    {
                        ModbusToVariable((ushort)ModbusOperation.WriteSingleCoil);
                    }

                    TCPCase15();
                    _writeCount++;
                    break;
                case (byte)ModbusOperation.WriteSingleHoldingRegister:
                    startRegister = (ushort)((_tcpRxBuf[addr_high] * 0x100) + (_tcpRxBuf[addr_low]));

                    if (startRegister > MaxInt)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    tempNo = (ushort)((_tcpRxBuf[quantity_high] << 8) | _tcpRxBuf[quantity_low]);

                    if (_fbInDataBuf[startRegister] != tempNo)
                    {
                        _fbInDataBuf[startRegister] = tempNo;
                        _isDataChngd = true;
                    }
                    if (_isDataChngd)
                    {
                        ModbusToVariable((ushort)ModbusOperation.WriteSingleHoldingRegister);
                    }
                    TCPCase6();
                    _writeCount++;
                    break;
                case (byte)ModbusOperation.WriteMultipleCoils:
                    reqRegister = (ushort)((_tcpRxBuf[10] * 0x100) + _tcpRxBuf[11]);
                    startRegister = (ushort)((_tcpRxBuf[8] * 0x100) + (_tcpRxBuf[9]));
                    procRegister = (ushort)(MaxBits - reqRegister);

                    if (startRegister > MaxBits)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress; ;
                    }
                    if (reqRegister > procRegister)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress; ;
                    }
                    if ((reqRegister > MaxBits) || (reqRegister == 0))
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress; ;
                    }

                    for (temp0 = 0; temp0 < reqRegister; temp0++)
                    {
                        tempNo = temp0;

                        modBytePos = (ushort)(tempNo / 8);
                        bitPos = (ushort)(tempNo % 8);
                        bitPos = BitValue(bitPos,1);

                        if ((_tcpRxBuf[13 + modBytePos] & bitPos) == 0)
                        {
                            srcBit = 0;
                        }
                        else
                        {
                            srcBit = 0xff;
                        }

                        tempNo = (ushort)(startRegister + temp0);

                        modBytePos = (ushort)(tempNo / 8);
                        bitPos = (ushort)(tempNo % 8);
                        bitPos = BitValue(bitPos,1);

                        if (srcBit == 0xff)
                        {
                            destBit = (byte)(_fbCoilBuf[modBytePos] | bitPos);
                        }
                        else
                        {
                            destBit = (byte)(_fbCoilBuf[modBytePos] & ((bitPos) ^ 0xff));
                        }

                        if (destBit != _fbCoilBuf[modBytePos])
                        {
                            _fbCoilBuf[modBytePos] = destBit;
                            _isDataChngd = true;
                        }

                    }
                    if (_isDataChngd)
                    {
                        ModbusToVariable((ushort)ModbusOperation.WriteMultipleCoils);
                    }

                    // Send response
                    TCPCase5();
                    _writeCount++;
                    break;
                case (byte)ModbusOperation.WriteMultipleHoldingRegisters:
                    reqRegister = (ushort)((_tcpRxBuf[quantity_high] * 0x100) + _tcpRxBuf[quantity_low]);
                    startRegister = (ushort)((_tcpRxBuf[addr_high] * 0x100) + (_tcpRxBuf[addr_low]));

                    procRegister = (ushort)(MaxInt - startRegister);

                    if (startRegister > MaxInt)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > procRegister)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    if (reqRegister > MaxPresetMultipleRegister)
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataAddress;
                    }
                    // Check for valid byte count
                    if (_tcpRxBuf[byte_count] != 0)
                    {
                        for (temp0 = 0; temp0 < reqRegister; temp0++)
                        {
                            bytHi = _tcpRxBuf[data_high + (2 * temp0)];
                            bytLo = _tcpRxBuf[data_low + (2 * temp0)];

                            tempNo = (ushort)((bytHi << 8) | bytLo);

                            if (_fbInDataBuf[startRegister + temp0] != tempNo)
                            {
                                _fbInDataBuf[startRegister + temp0] = tempNo;
                                _isDataChngd = true;
                            }
                        }
                    }
                    else
                    {
                        return (ushort)ModbusExceptionCodes.IllegalDataValue;
                    }

                    if (_isDataChngd)
                    {
                        ModbusToVariable((ushort)ModbusOperation.WriteMultipleHoldingRegisters);
                    }

                    TCPCase16();
                    _writeCount++;
                    break;
                default:
                    break;
            }
            return 0;
        }
        private void TCPCase1(ushort reqRegister)
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = _tcpRxBuf[4];
            _tcpTxBuf[5] = (byte)((_tcpRxBuf[11] * 2) + 3);

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Number of bytes more
            _tcpTxBuf[8] = (byte)(_tcpRxBuf[11] * 2);

            // Calculate number of bytes to send
            _tcpChrCnt = (byte)((reqRegister * 2) + 9);

            tcp_send_data_flag = true;
        }
        private void TCPCase2(ushort reqRegister)
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = _tcpRxBuf[4];
            _tcpTxBuf[5] = (byte)((_tcpRxBuf[11] * 2) + 3);

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Number of bytes more
            _tcpTxBuf[8] = (byte)(_tcpRxBuf[11] * 2);

            // Calculate number of bytes to send
            _tcpChrCnt = (byte)((reqRegister * 2) + 9);

            tcp_send_data_flag = true;
        }
        private void TCPCase3(ushort startRegister, ushort reqRegister)
        {
            ushort i = 0;
            ushort j;
            ushort n;

            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            _tcpTxBuf[4] = _tcpRxBuf[4];
            _tcpTxBuf[5] = (byte)((_tcpRxBuf[11] * 2) + 3);

            _tcpTxBuf[6] = _tcpRxBuf[6];
            _tcpTxBuf[7] = _tcpRxBuf[7];

            _tcpTxBuf[8] = (byte)(_tcpRxBuf[11] * 2);

            _tcpChrCnt = 9;

            for (j = 0; j < reqRegister; j++)
            {
                n = _fbInDataBuf[startRegister + j];

                _tcpTxBuf[9 + i] = (byte)((n >> 8) & 0x00ff);
                i++;
                _tcpTxBuf[9 + i] = (byte)(n & 0x00ff);
                i++;
                _tcpChrCnt += 2;
            }
            tcp_send_data_flag = true;
        }
        private void TCPCase4(ushort startRegister, ushort reqRegister)
        {
            ushort i = 0;
            ushort j;
            ushort temp;

            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            _tcpTxBuf[4] = _tcpRxBuf[4];
            _tcpTxBuf[5] = (byte)((_tcpRxBuf[11] * 2) + 3);

            _tcpTxBuf[6] = _tcpRxBuf[6];
            _tcpTxBuf[7] = _tcpRxBuf[7];

            _tcpTxBuf[8] = (byte)(_tcpRxBuf[11] * 2);

            _tcpChrCnt = 9;

            for (j = 0; j < reqRegister; j++)
            {
                temp = _fbInDataBuf[startRegister + j];

                _tcpTxBuf[9 + i] = (byte)((temp >> 8) & 0x00ff);
                i++;
                _tcpTxBuf[9 + i] = (byte)(temp & 0x00ff);
                i++;
                _tcpChrCnt += 2;
            }
            tcp_send_data_flag = true;
        }
        private void TCPCase5()
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = 0; //g_userData[4];
            _tcpTxBuf[5] = 6; //g_userData[5];

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Starting Register Address High
            _tcpTxBuf[8] = _tcpRxBuf[8];

            // Starting Register Address Low
            _tcpTxBuf[9] = _tcpRxBuf[9];

            // Number Of Register High
            _tcpTxBuf[10] = _tcpRxBuf[10];

            // Number Of Register Low
            _tcpTxBuf[11] = _tcpRxBuf[11];

            _tcpChrCnt = 12;

            tcp_send_data_flag = true;
        }
        private void TCPCase6()
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = 0; //g_userData[4];
            _tcpTxBuf[5] = 6; //g_userData[5];

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Starting Register Address High
            _tcpTxBuf[8] = _tcpRxBuf[8];

            // Starting Register Address Low
            _tcpTxBuf[9] = _tcpRxBuf[9];

            // Number Of Register High
            _tcpTxBuf[10] = _tcpRxBuf[10];

            // Number Of Register Low
            _tcpTxBuf[11] = _tcpRxBuf[11];

            _tcpChrCnt = 12;

            tcp_send_data_flag = true;
        }
        private void TCPCase15()
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = _tcpRxBuf[4];
            _tcpTxBuf[5] = _tcpRxBuf[5];

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Coil address high order
            _tcpTxBuf[8] = _tcpRxBuf[8];

            // Coil address low order
            _tcpTxBuf[9] = _tcpRxBuf[9];

            // Force data high order
            _tcpTxBuf[10] = _tcpRxBuf[10];

            // Force data low order
            _tcpTxBuf[11] = _tcpRxBuf[11];

            _tcpChrCnt = 12;

            tcp_send_data_flag = true;
        }
        private void TCPCase16()
        {
            // Transaction Identifier
            _tcpTxBuf[0] = _tcpRxBuf[0];
            _tcpTxBuf[1] = _tcpRxBuf[1];

            // Protocol Identifie
            _tcpTxBuf[2] = _tcpRxBuf[2];
            _tcpTxBuf[3] = _tcpRxBuf[3];

            // Length Field
            _tcpTxBuf[4] = 0; //g_userData[4];
            _tcpTxBuf[5] = 6; //g_userData[5];

            // Unit ID
            _tcpTxBuf[6] = _tcpRxBuf[6];

            // Function Code
            _tcpTxBuf[7] = _tcpRxBuf[7];

            // Starting Register Address High
            _tcpTxBuf[8] = _tcpRxBuf[8];

            // Starting Register Address Low
            _tcpTxBuf[9] = _tcpRxBuf[9];

            // Number Of Register High
            _tcpTxBuf[10] = _tcpRxBuf[10];

            // Number Of Register Low
            _tcpTxBuf[11] = _tcpRxBuf[11];

            _tcpChrCnt = 12;

            tcp_send_data_flag = true;
        }
        private void ModbusToVariable(ushort fcunctionCode)
        {
        }
    }
}
