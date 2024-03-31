using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	internal class RequestQueueItem
	{
		public byte[] Request { get; set; }

		public Func<IReadOnlyList<byte>, bool> ValidateResponseComplete { get; set; }

		public TaskCompletionSource<IReadOnlyList<byte>> TaskCompletionSource { get; set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
	}
}
