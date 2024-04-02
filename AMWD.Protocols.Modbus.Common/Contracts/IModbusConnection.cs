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
		/// Gets or sets the idle time after that the connection is closed.
		/// </summary>
		/// <remarks>
		/// Set to <see cref="Timeout.InfiniteTimeSpan"/> to disable idle closing the connection.
		/// <br/>
		/// Set to <see cref="TimeSpan.Zero"/> to close the connection immediately after each request.
		/// </remarks>
		TimeSpan IdleTimeout { get; set; }

		/// <summary>
		/// Gets or sets the maximum time until the connect attempt is given up.
		/// </summary>
		TimeSpan ConnectTimeout { get; set; }

		/// <summary>
		/// Gets or sets the receive time out value of the connection.
		/// </summary>
		TimeSpan ReadTimeout { get; set; }

		/// <summary>
		/// Gets or sets the send time out value of the connection.
		/// </summary>
		TimeSpan WriteTimeout { get; set; }

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
