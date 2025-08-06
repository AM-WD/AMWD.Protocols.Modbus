using System;
using System.Collections;
using System.Collections.Generic;

namespace AMWD.Common.Cli
{
	/// <summary>
	/// Walks through an <see cref="IEnumerable{T}"/> and allows retrieving additional items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// Initialises a new instance of the <see cref="EnumerableWalker{T}"/> class.
	/// </remarks>
	/// <param name="array">The array to walk though.</param>
	internal class EnumerableWalker<T>(IEnumerable<T> array)
		: IEnumerable<T> where T : class
	{
		private readonly IEnumerable<T> _array = array ?? throw new ArgumentNullException(nameof(array));
		private IEnumerator<T> _enumerator;

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			_enumerator = _array.GetEnumerator();
			return _enumerator;
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator GetEnumerator()
		{
			_enumerator = _array.GetEnumerator();
			return _enumerator;
		}

		/// <summary>
		/// Gets the next item.
		/// </summary>
		/// <returns>The next item.</returns>
		public T GetNext()
		{
			if (_enumerator.MoveNext())
			{
				return _enumerator.Current;
			}
			else
			{
				return default;
			}
		}
	}
}
