namespace ModScope.ModbusMaster;

/// <summary>
/// Standard Modbus function codes supported by ModScope.
/// </summary>
public enum ModbusFunctionCode : byte
{
    /// <summary>Read Discrete Coils (FC01) - 1 bit read.</summary>
    ReadCoils = 0x01,

    /// <summary>Read Discrete Inputs (FC02) - 1 bit read-only.</summary>
    ReadDiscreteInputs = 0x02,

    /// <summary>Read Holding Registers (FC03) - 16 bit read/write.</summary>
    ReadHoldingRegisters = 0x03,

    /// <summary>Read Input Registers (FC04) - 16 bit read-only.</summary>
    ReadInputRegisters = 0x04,

    /// <summary>Write Single Coil (FC05) - 1 bit write.</summary>
    WriteSingleCoil = 0x05,

    /// <summary>Write Single Holding Register (FC06) - 16 bit write.</summary>
    WriteSingleRegister = 0x06,

    /// <summary>Write Multiple Coils (FC15) - multiple bits write.</summary>
    WriteMultipleCoils = 0x0F,

    /// <summary>Write Multiple Holding Registers (FC16) - multiple 16-bit registers write.</summary>
    WriteMultipleRegisters = 0x10,

    /// <summary>Mask Write Register (FC22) - logical AND/OR write.</summary>
    MaskWriteRegister = 0x16,

    /// <summary>Read/Write Multiple Registers (FC23).</summary>
    ReadWriteMultipleRegisters = 0x17
}

/// <summary>
/// Standard Modbus protocol exception codes returned by slaves.
/// </summary>
public enum ModbusExceptionCode : byte
{
    /// <summary>The function code received in the query is not an allowable action for the server.</summary>
    IllegalFunction = 0x01,

    /// <summary>The data address received in the query is not an allowable address for the server.</summary>
    IllegalDataAddress = 0x02,

    /// <summary>A value contained in the query data field is not an allowable value for server.</summary>
    IllegalDataValue = 0x03,

    /// <summary>An unrecoverable error occurred while the server was attempting to perform the requested action.</summary>
    SlaveDeviceFailure = 0x04,

    /// <summary>The server has accepted the request and is processing it, but a long duration of time will be required to do so.</summary>
    Acknowledge = 0x05,

    /// <summary>The server is engaged in processing a long–duration program command.</summary>
    SlaveDeviceBusy = 0x06,

    /// <summary>The server cannot perform the program function received in the query.</summary>
    NegativeAcknowledge = 0x07,

    /// <summary>The server detected a parity error in memory reading extended memory.</summary>
    MemoryParityError = 0x08,

    /// <summary>Specialized gateway path was unavailable.</summary>
    GatewayPathUnavailable = 0x0A,

    /// <summary>The targeted device failed to respond across a gateway.</summary>
    GatewayTargetDeviceFailedToRespond = 0x0B
}
