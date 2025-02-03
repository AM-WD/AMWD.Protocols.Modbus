using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Tcp.Utils;

namespace System.IO
{
	internal static class StreamExtensions
	{
		public static async Task<byte[]> ReadExpectedBytesAsync(this Stream stream, int expectedBytes, CancellationToken cancellationToken = default)
		{
			byte[] buffer = new byte[expectedBytes];
			int offset = 0;
			do
			{
				int count = await stream.ReadAsync(buffer, offset, expectedBytes - offset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (count < 1)
					throw new EndOfStreamException();

				offset += count;
			}
			while (offset < expectedBytes && !cancellationToken.IsCancellationRequested);

			cancellationToken.ThrowIfCancellationRequested();
			return buffer;
		}

		public static async Task<byte[]> ReadExpectedBytesAsync(this NetworkStreamWrapper stream, int expectedBytes, CancellationToken cancellationToken = default)
		{
			byte[] buffer = new byte[expectedBytes];
			int offset = 0;
			do
			{
				int count = await stream.ReadAsync(buffer, offset, expectedBytes - offset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (count < 1)
					throw new EndOfStreamException();

				offset += count;
			}
			while (offset < expectedBytes && !cancellationToken.IsCancellationRequested);

			cancellationToken.ThrowIfCancellationRequested();
			return buffer;
		}
	}
}
