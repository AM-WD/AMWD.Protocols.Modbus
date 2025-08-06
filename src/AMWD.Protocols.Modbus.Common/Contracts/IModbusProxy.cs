using System;
using System.Threading.Tasks;
using System.Threading;

namespace AMWD.Protocols.Modbus.Common.Contracts
{
	/// <summary>
	/// Represents a Modbus proxy.
	/// </summary>
	public interface IModbusProxy : IDisposable
	{
		/// <summary>
		/// Starts the proxy.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		Task StartAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Stops the proxy.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		Task StopAsync(CancellationToken cancellationToken = default);
	}
}
