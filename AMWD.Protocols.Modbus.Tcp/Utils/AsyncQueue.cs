using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
	// ============================================================================================================================= //
	// Source: https://git.am-wd.de/am.wd/common/-/blob/d4b390ad911ce302cc371bb2121fa9c31db1674a/AMWD.Common/Utilities/AsyncQueue.cs //
	// ============================================================================================================================= //
	[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class AsyncQueue<T>
	{
		private readonly Queue<T> _queue = new();

		private TaskCompletionSource<bool> _dequeueTcs = new();
		private readonly TaskCompletionSource<bool> _availableTcs = new();

		public T Dequeue()
		{
			lock (_queue)
			{
				return _queue.Dequeue();
			}
		}

		public void Enqueue(T item)
		{
			lock (_queue)
			{
				_queue.Enqueue(item);
				SetToken(_dequeueTcs);
				SetToken(_availableTcs);
			}
		}

		public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
		{
			while (true)
			{
				TaskCompletionSource<bool> internalDequeueTcs;
				lock (_queue)
				{
					if (_queue.Count > 0)
						return _queue.Dequeue();

					internalDequeueTcs = ResetToken(ref _dequeueTcs);
				}

				await WaitAsync(internalDequeueTcs, cancellationToken).ConfigureAwait(false);
			}
		}

		public bool TryDequeue(out T result)
		{
			try
			{
				result = Dequeue();
				return true;
			}
			catch
			{
				result = default;
				return false;
			}
		}

		public bool Remove(T item)
		{
			lock (_queue)
			{
				var copy = new Queue<T>(_queue);
				_queue.Clear();

				bool found = false;
				int count = copy.Count;
				for (int i = 0; i < count; i++)
				{
					var element = copy.Dequeue();
					if (found)
					{
						_queue.Enqueue(element);
						continue;
					}

					if ((element == null && item == null) || element?.Equals(item) == true)
					{
						found = true;
						continue;
					}

					_queue.Enqueue(element);
				}

				return found;
			}
		}

		private static void SetToken(TaskCompletionSource<bool> tcs)
		{
			tcs.TrySetResult(true);
		}

		private static TaskCompletionSource<bool> ResetToken(ref TaskCompletionSource<bool> tcs)
		{
			if (tcs.Task.IsCompleted)
			{
				tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			return tcs;
		}

		private static async Task WaitAsync(TaskCompletionSource<bool> tcs, CancellationToken cancellationToken)
		{
			if (await Task.WhenAny(tcs.Task, Task.Delay(-1, cancellationToken)) == tcs.Task)
			{
				await tcs.Task.ConfigureAwait(false);
				return;
			}

			cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
