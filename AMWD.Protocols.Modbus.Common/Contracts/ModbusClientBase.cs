using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace AMWD.Protocols.Modbus.Common.Contracts
{
	/// <summary>
	/// Base implementation of a Modbus client.
	/// </summary>
	public abstract class ModbusClientBase : IDisposable
	{
		private bool _isDisposed;

		/// <summary>
		/// Gets or sets a value indicating whether the connection should be disposed of by <see cref="Dispose()"/>.
		/// </summary>
		protected readonly bool disposeConnection;

		/// <summary>
		/// Gets or sets the <see cref="IModbusConnection"/> responsible for invoking the requests.
		/// </summary>
		protected readonly IModbusConnection connection;

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusClientBase"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		public ModbusClientBase(IModbusConnection connection)
			: this(connection, true)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusClientBase"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		/// <param name="disposeConnection">
		/// <see langword="true"/> if the connection should be disposed of by Dispose(),
		/// <see langword="false"/> otherwise if you inted to reuse the connection.
		/// </param>
		public ModbusClientBase(IModbusConnection connection, bool disposeConnection)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.disposeConnection = disposeConnection;
		}

		/// <summary>
		/// Gets a value indicating whether the client is connected.
		/// </summary>
		public bool IsConnected => connection.IsConnected;

		/// <summary>
		/// Gets or sets the protocol type to use.
		/// </summary>
		/// <remarks>
		/// The default protocol used by the client should be initialized in the constructor.
		/// </remarks>
		public abstract IModbusProtocol Protocol { get; set; }

		/// <summary>
		/// Starts the connection to the remote endpoint.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>An awaitable <see cref="Task"/>.</returns>
		public virtual Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			Assertions(false);
			return connection.ConnectAsync(cancellationToken);
		}

		/// <summary>
		/// Stops the connection to the remote endpoint.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>An awaitable <see cref="Task"/>.</returns>
		public virtual Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
			Assertions(false);
			return connection.DisconnectAsync(cancellationToken);
		}

		/// <summary>
		/// Reads multiple <see cref="Coil"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of coils to read.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A list of <see cref="Coil"/>s.</returns>
		public virtual async Task<IReadOnlyList<Coil>> ReadCoilsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeReadCoils(unitId, startAddress, count);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			// The protocol processes complete bytes from the response.
			// So reduce to the actual coil count.
			var coils = Protocol.DeserializeReadCoils(response).Take(count);
			foreach (var coil in coils)
				coil.Address += startAddress;

			return coils.ToList();
		}

		/// <summary>
		/// Reads multiple <see cref="DiscreteInput"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of inputs to read.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A list of <see cref="DiscreteInput"/>s.</returns>
		public virtual async Task<IReadOnlyList<DiscreteInput>> ReadDiscreteInputsAsync(byte unitId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeReadDiscreteInputs(unitId, startAddress, count);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			// The protocol processes complete bytes from the response.
			// So reduce to the actual discrete input count.
			var discreteInputs = Protocol.DeserializeReadDiscreteInputs(response).Take(count);
			foreach (var discreteInput in discreteInputs)
				discreteInput.Address += startAddress;

			return discreteInputs.ToList();
		}

		/// <summary>
		/// Reads multiple <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of registers to read.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A list of <see cref="HoldingRegister"/>s.</returns>
		public virtual async Task<IReadOnlyList<HoldingRegister>> ReadHoldingRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeReadHoldingRegisters(unitId, startAddress, count);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var holdingRegisters = Protocol.DeserializeReadHoldingRegisters(response).ToList();
			foreach (var holdingRegister in holdingRegisters)
				holdingRegister.Address += startAddress;

			return holdingRegisters;
		}

		/// <summary>
		/// Reads multiple <see cref="InputRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of registers to read.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A list of <see cref="InputRegister"/>s.</returns>
		public virtual async Task<IReadOnlyList<InputRegister>> ReadInputRegistersAsync(byte unitId, ushort startAddress, ushort count, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeReadInputRegisters(unitId, startAddress, count);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var inputRegisters = Protocol.DeserializeReadInputRegisters(response).ToList();
			foreach (var inputRegister in inputRegisters)
				inputRegister.Address += startAddress;

			return inputRegisters;
		}

		/// <summary>
		/// Writes a single <see cref="Coil"/>.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="coil">The coil to write.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns><see langword="true"/> on success, otherwise <see langword="false"/>.</returns>
		public virtual async Task<bool> WriteSingleCoilAsync(byte unitId, Coil coil, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeWriteSingleCoil(unitId, coil);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var result = Protocol.DeserializeWriteSingleCoil(response);

			return coil.Address == result.Address
				&& coil.Value == result.Value;
		}

		/// <summary>
		/// Writs a single <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="register">The register to write.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns><see langword="true"/> on success, otherwise <see langword="false"/>.</returns>
		public virtual async Task<bool> WriteSingleHoldingRegisterAsync(byte unitId, HoldingRegister register, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeWriteSingleHoldingRegister(unitId, register);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var result = Protocol.DeserializeWriteSingleHoldingRegister(response);

			return register.Address == result.Address
				&& register.Value == result.Value;
		}

		/// <summary>
		/// Writes multiple <see cref="Coil"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="coils">The coils to write.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns><see langword="true"/> on success, otherwise <see langword="false"/>.</returns>
		public virtual async Task<bool> WriteMultipleCoilsAsync(byte unitId, IReadOnlyList<Coil> coils, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeWriteMultipleCoils(unitId, coils);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var (firstAddress, count) = Protocol.DeserializeWriteMultipleCoils(response);

			return coils.Count == count && coils.OrderBy(c => c.Address).First().Address == firstAddress;
		}

		/// <summary>
		/// Writes multiple <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="registers">The registers to write.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns><see langword="true"/> on success, otherwise <see langword="false"/>.</returns>
		public virtual async Task<bool> WriteMultipleHoldingRegistersAsync(byte unitId, IReadOnlyList<HoldingRegister> registers, CancellationToken cancellationToken = default)
		{
			Assertions();

			var request = Protocol.SerializeWriteMultipleHoldingRegisters(unitId, registers);
			var response = await connection.InvokeAsync(request, Protocol.CheckResponseComplete, cancellationToken);
			Protocol.ValidateResponse(request, response);

			var (firstAddress, count) = Protocol.DeserializeWriteMultipleHoldingRegisters(response);

			return registers.Count == count && registers.OrderBy(c => c.Address).First().Address == firstAddress;
		}

		/// <summary>
		/// Releases all managed and unmanaged resources used by the <see cref="ModbusClientBase"/>.
		/// </summary>
		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public override string ToString()
			=> $"Modbus client using {Protocol.Name} protocol to connect via {connection.Name}";

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="ModbusClientBase"/>
		/// and optionally also discards the managed resources.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				_isDisposed = true;

				if (disposeConnection)
					connection.Dispose();
			}
		}

		/// <summary>
		/// Performs basic assertions.
		/// </summary>
		protected virtual void Assertions(bool checkConnected = true)
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif

#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(Protocol);
#else
			if (Protocol == null)
				throw new ArgumentNullException(nameof(Protocol));
#endif

			if (!checkConnected)
				return;

			if (!IsConnected)
				throw new ApplicationException($"Connection is not open");
		}
	}
}
