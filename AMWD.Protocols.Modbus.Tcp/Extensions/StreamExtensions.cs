using System.Threading;
using System.Threading.Tasks;

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
				int count = await stream.ReadAsync(buffer, offset, expectedBytes - offset, cancellationToken).ConfigureAwait(false);
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
