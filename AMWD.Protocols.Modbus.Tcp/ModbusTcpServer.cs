using System;
using System.Net;
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Events;
using AMWD.Protocols.Modbus.Common.Utils;

namespace AMWD.Protocols.Modbus.Tcp
{
	/// <summary>
	/// Implements a Modbus TCP server proxying all requests to a virtual Modbus client.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ModbusTcpServer : ModbusTcpProxy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusTcpServer"/> class.
		/// </summary>
		/// <param name="listenAddress">The <see cref="IPAddress"/> to listen on.</param>
		public ModbusTcpServer(IPAddress listenAddress)
			: base(new VirtualModbusClient(), listenAddress)
		{
			TypedClient.CoilWritten += (sender, e) => CoilWritten?.Invoke(this, e);
			TypedClient.RegisterWritten += (sender, e) => RegisterWritten?.Invoke(this, e);
		}

		#region Events

		/// <summary>
		/// Indicates that a <see cref="Coil"/>-value received through a remote client has been written.
		/// </summary>
		public event EventHandler<CoilWrittenEventArgs> CoilWritten;

		/// <summary>
		/// Indicates that a <see cref="HoldingRegister"/>-value received from a remote client has been written.
		/// </summary>
		public event EventHandler<RegisterWrittenEventArgs> RegisterWritten;

		#endregion Events

		#region Properties

		internal VirtualModbusClient TypedClient
			=> Client as VirtualModbusClient;

		#endregion Properties

		#region Device Handling

		/// <inheritdoc cref="VirtualModbusClient.AddDevice(byte)"/>
		public bool AddDevice(byte unitId)
			=> TypedClient.AddDevice(unitId);

		/// <inheritdoc cref="VirtualModbusClient.RemoveDevice(byte)"/>
		public bool RemoveDevice(byte unitId)
			=> TypedClient.RemoveDevice(unitId);

		#endregion Device Handling

		#region Entity Handling

		/// <inheritdoc cref="VirtualModbusClient.GetCoil(byte, ushort)"/>
		public Coil GetCoil(byte unitId, ushort address)
			=> TypedClient.GetCoil(unitId, address);

		/// <inheritdoc cref="VirtualModbusClient.SetCoil(byte, Coil)"/>
		public void SetCoil(byte unitId, Coil coil)
			=> TypedClient.SetCoil(unitId, coil);

		/// <inheritdoc cref="VirtualModbusClient.GetDiscreteInput(byte, ushort)"/>
		public DiscreteInput GetDiscreteInput(byte unitId, ushort address)
			=> TypedClient.GetDiscreteInput(unitId, address);

		/// <inheritdoc cref="VirtualModbusClient.SetDiscreteInput(byte, DiscreteInput)"/>
		public void SetDiscreteInput(byte unitId, DiscreteInput discreteInput)
			=> TypedClient.SetDiscreteInput(unitId, discreteInput);

		/// <inheritdoc cref="VirtualModbusClient.GetHoldingRegister(byte, ushort)"/>
		public HoldingRegister GetHoldingRegister(byte unitId, ushort address)
			=> TypedClient.GetHoldingRegister(unitId, address);

		/// <inheritdoc cref="VirtualModbusClient.SetHoldingRegister(byte, HoldingRegister)"/>
		public void SetHoldingRegister(byte unitId, HoldingRegister holdingRegister)
			=> TypedClient.SetHoldingRegister(unitId, holdingRegister);

		/// <inheritdoc cref="VirtualModbusClient.GetInputRegister(byte, ushort)"/>
		public InputRegister GetInputRegister(byte unitId, ushort address)
			=> TypedClient.GetInputRegister(unitId, address);

		/// <inheritdoc cref="VirtualModbusClient.SetInputRegister(byte, InputRegister)"/>
		public void SetInputRegister(byte unitId, InputRegister inputRegister)
			=> TypedClient.SetInputRegister(unitId, inputRegister);

		#endregion Entity Handling
	}
}
