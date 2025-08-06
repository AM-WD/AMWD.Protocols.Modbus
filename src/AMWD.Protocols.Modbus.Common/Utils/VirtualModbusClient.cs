using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Events;
using AMWD.Protocols.Modbus.Common.Models;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Common.Utils
{
	/// <summary>
	/// Implements a virtual Modbus client.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class VirtualModbusClient : ModbusClientBase
	{
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualModbusClient"/> class.
		/// </summary>
		/// <remarks><strong>DO NOT MODIFY</strong> connection or protocol.</remarks>
		public VirtualModbusClient()
			: base(new VirtualConnection())
		{
			Protocol = new VirtualProtocol();

			TypedProtocol.CoilWritten += (sender, e) => CoilWritten?.Invoke(this, e);
			TypedProtocol.RegisterWritten += (sender, e) => RegisterWritten?.Invoke(this, e);
		}

		#endregion Constructor

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

		internal VirtualProtocol TypedProtocol
			=> Protocol as VirtualProtocol;

		#endregion Properties

		#region Device Handling

		/// <summary>
		/// Adds a device to the virtual client.
		/// </summary>
		/// <param name="unitId">The unit id of the device.</param>
		/// <returns><see langword="true"/> if the device was added successfully, <see langword="false"/> otherwise.</returns>
		public bool AddDevice(byte unitId)
			=> TypedProtocol.AddDevice(unitId);

		/// <summary>
		/// Removes a device from the virtual client.
		/// </summary>
		/// <param name="unitId">The unit id of the device.</param>
		/// <returns><see langword="true"/> if the device was removed successfully, <see langword="false"/> otherwise.</returns>
		public bool RemoveDevice(byte unitId)
			=> TypedProtocol.RemoveDevice(unitId);

		#endregion Device Handling

		#region Entity Handling

		/// <summary>
		/// Gets a <see cref="Coil"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="Coil"/>.</param>
		public Coil GetCoil(byte unitId, ushort address)
			=> TypedProtocol.GetCoil(unitId, address);

		/// <summary>
		/// Sets a <see cref="Coil"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="coil">The <see cref="Coil"/> to set.</param>
		public void SetCoil(byte unitId, Coil coil)
			=> TypedProtocol.SetCoil(unitId, coil);

		/// <summary>
		/// Gets a <see cref="DiscreteInput"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="DiscreteInput"/>.</param>
		public DiscreteInput GetDiscreteInput(byte unitId, ushort address)
			=> TypedProtocol.GetDiscreteInput(unitId, address);

		/// <summary>
		/// Sets a <see cref="DiscreteInput"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="discreteInput">The <see cref="DiscreteInput"/> to set.</param>
		public void SetDiscreteInput(byte unitId, DiscreteInput discreteInput)
			=> TypedProtocol.SetDiscreteInput(unitId, discreteInput);

		/// <summary>
		/// Gets a <see cref="HoldingRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="HoldingRegister"/>.</param>
		public HoldingRegister GetHoldingRegister(byte unitId, ushort address)
			=> TypedProtocol.GetHoldingRegister(unitId, address);

		/// <summary>
		/// Sets a <see cref="HoldingRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="holdingRegister">The <see cref="HoldingRegister"/> to set.</param>
		public void SetHoldingRegister(byte unitId, HoldingRegister holdingRegister)
			=> TypedProtocol.SetHoldingRegister(unitId, holdingRegister);

		/// <summary>
		/// Gets a <see cref="InputRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="InputRegister"/>.</param>
		public InputRegister GetInputRegister(byte unitId, ushort address)
			=> TypedProtocol.GetInputRegister(unitId, address);

		/// <summary>
		/// Sets a <see cref="InputRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="inputRegister">The <see cref="InputRegister"/> to set.</param>
		public void SetInputRegister(byte unitId, InputRegister inputRegister)
			=> TypedProtocol.SetInputRegister(unitId, inputRegister);

		#endregion Entity Handling

		#region Methods

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			TypedProtocol.Dispose();
			base.Dispose(disposing);
		}

		#endregion Methods

		#region Connection

		internal class VirtualConnection : IModbusConnection
		{
			public string Name => nameof(VirtualConnection);

			public TimeSpan IdleTimeout { get; set; }

			public TimeSpan ConnectTimeout { get; set; }

			public TimeSpan ReadTimeout { get; set; }

			public TimeSpan WriteTimeout { get; set; }

			public void Dispose()
			{ /* nothing to do */ }

			public Task<IReadOnlyList<byte>> InvokeAsync(
				IReadOnlyList<byte> request,
				Func<IReadOnlyList<byte>, bool> validateResponseComplete,
				CancellationToken cancellationToken = default) => Task.FromResult(request);
		}

		#endregion Connection
	}
}
