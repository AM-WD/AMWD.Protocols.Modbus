using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="NetworkStream" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class NetworkStreamWrapper : IDisposable
	{
		private readonly NetworkStream _stream;

		[Obsolete("Constructor only for mocking on UnitTests!", error: true)]
		public NetworkStreamWrapper()
		{ }

		public NetworkStreamWrapper(NetworkStream stream)
		{
			_stream = stream;
		}

		public virtual void Dispose()
			=> _stream.Dispose();

		/// <inheritdoc cref="Stream.ReadAsync(byte[], int, int, CancellationToken)" />
		public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
			=> _stream.ReadAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
		/// <inheritdoc cref="NetworkStream.ReadAsync(Memory{byte}, CancellationToken)" />
		public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> _stream.ReadAsync(buffer, cancellationToken);
#endif

		/// <inheritdoc cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
		public virtual Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
			=> _stream.WriteAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
		/// <inheritdoc cref="NetworkStream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)"/>
		public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
			=> _stream.WriteAsync(buffer, cancellationToken);
#endif

		/// <inheritdoc cref="Stream.FlushAsync(CancellationToken)"/>
		public virtual Task FlushAsync(CancellationToken cancellationToken = default)
			=> _stream.FlushAsync(cancellationToken);
	}
}
