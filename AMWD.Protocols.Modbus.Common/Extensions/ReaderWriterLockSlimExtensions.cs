namespace System.Threading
{
	// ================================================================================================================================================== //
	// Source: https://git.am-wd.de/am-wd/common/-/blob/d4b390ad911ce302cc371bb2121fa9c31db1674a/AMWD.Common/Extensions/ReaderWriterLockSlimExtensions.cs //
	// ================================================================================================================================================== //
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class ReaderWriterLockSlimExtensions
	{
		/// <summary>
		/// Acquires a read lock on a lock object that can be released with an
		/// <see cref="IDisposable"/> instance.
		/// </summary>
		/// <param name="rwLock">The lock object.</param>
		/// <param name="timeoutMilliseconds">The number of milliseconds to wait, or -1
		///   (<see cref="Timeout.Infinite"/>) to wait indefinitely.</param>
		/// <returns>An <see cref="IDisposable"/> instance to release the lock.</returns>
		public static IDisposable GetReadLock(this ReaderWriterLockSlim rwLock, int timeoutMilliseconds = -1)
		{
			if (!rwLock.TryEnterReadLock(timeoutMilliseconds))
				throw new TimeoutException("The read lock could not be acquired.");

			return new DisposableReadWriteLock(rwLock, LockMode.Read);
		}

		/// <summary>
		/// Acquires a upgradeable read lock on a lock object that can be released with an
		/// <see cref="IDisposable"/> instance. The lock can be upgraded to a write lock temporarily
		/// with <see cref="GetWriteLock"/> or until the lock is released with
		/// <see cref="ReaderWriterLockSlim.EnterWriteLock"/> alone.
		/// </summary>
		/// <param name="rwLock">The lock object.</param>
		/// <param name="timeoutMilliseconds">The number of milliseconds to wait, or -1
		///   (<see cref="Timeout.Infinite"/>) to wait indefinitely.</param>
		/// <returns>An <see cref="IDisposable"/> instance to release the lock. If the lock was
		///   upgraded to a write lock, that will be released as well.</returns>
		public static IDisposable GetUpgradeableReadLock(this ReaderWriterLockSlim rwLock, int timeoutMilliseconds = -1)
		{
			if (!rwLock.TryEnterUpgradeableReadLock(timeoutMilliseconds))
				throw new TimeoutException("The upgradeable read lock could not be acquired.");

			return new DisposableReadWriteLock(rwLock, LockMode.Upgradable);
		}

		/// <summary>
		/// Acquires a write lock on a lock object that can be released with an
		/// <see cref="IDisposable"/> instance.
		/// </summary>
		/// <param name="rwLock">The lock object.</param>
		/// <param name="timeoutMilliseconds">The number of milliseconds to wait, or -1
		///   (<see cref="Timeout.Infinite"/>) to wait indefinitely.</param>
		/// <returns>An <see cref="IDisposable"/> instance to release the lock.</returns>
		public static IDisposable GetWriteLock(this ReaderWriterLockSlim rwLock, int timeoutMilliseconds = -1)
		{
			if (!rwLock.TryEnterWriteLock(timeoutMilliseconds))
				throw new TimeoutException("The write lock could not be acquired.");

			return new DisposableReadWriteLock(rwLock, LockMode.Write);
		}

		private struct DisposableReadWriteLock(ReaderWriterLockSlim rwLock, LockMode lockMode)
			: IDisposable
		{
			private readonly ReaderWriterLockSlim _rwLock = rwLock;
			private LockMode _lockMode = lockMode;

			public void Dispose()
			{
				if (_lockMode == LockMode.Read)
					_rwLock.ExitReadLock();

				if (_lockMode == LockMode.Upgradable && _rwLock.IsWriteLockHeld)   // Upgraded with EnterWriteLock alone
					_rwLock.ExitWriteLock();

				if (_lockMode == LockMode.Upgradable)
					_rwLock.ExitUpgradeableReadLock();

				if (_lockMode == LockMode.Write)
					_rwLock.ExitWriteLock();

				_lockMode = LockMode.None;
			}
		}

		private enum LockMode
		{
			None = 0,
			Read = 1,
			Upgradable = 2,
			Write = 3,
		}
	}
}
