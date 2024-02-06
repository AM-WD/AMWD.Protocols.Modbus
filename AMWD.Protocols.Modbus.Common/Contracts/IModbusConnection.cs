using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Common.Contracts
{
	/// <summary>
	/// Represents a Modbus connection.
	/// </summary>
	public interface IModbusConnection : IDisposable
	{
		/// <summary>
		/// The connection type name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the connection is open.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Opens the connection to the remote device.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>An awaitable <see cref="Task"/>.</returns>
		Task ConnectAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Closes the connection to the remote device.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>An awaitable <see cref="Task"/>.</returns>
		Task DisconnectAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Invokes a Modbus request.
		/// </summary>
		/// <param name="request">The Modbus request serialized in bytes.</param>
		/// <param name="validateResponseComplete">A function to validate whether the response is complete.</param>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		/// <returns>A list of <see cref="byte"/>s containing the response.</returns>
		Task<IReadOnlyList<byte>> InvokeAsync(IReadOnlyList<byte> request, Func<IReadOnlyList<byte>, bool> validateResponseComplete, CancellationToken cancellationToken = default);
	}
}
