using System;
using System.Collections.Generic;

namespace AMWD.Common.Cli
{
	/// <summary>
	/// Represents a named option.
	/// </summary>
	internal class Option
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="Option"/> class.
		/// </summary>
		/// <param name="name">The primary name of the option.</param>
		/// <param name="parameterCount">The number of additional parameters for this option.</param>
		internal Option(string name, int parameterCount)
		{
			Names = [name];
			ParameterCount = parameterCount;
		}

		/// <summary>
		/// Gets the names of this option.
		/// </summary>
		public List<string> Names { get; private set; }

		/// <summary>
		/// Gets the number of additional parameters for this option.
		/// </summary>
		public int ParameterCount { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this option is required.
		/// </summary>
		public bool IsRequired { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this option can only be specified once.
		/// </summary>
		public bool IsSingle { get; private set; }

		/// <summary>
		/// Gets the action to invoke when the option is set.
		/// </summary>
		public Action<Argument> Action { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this option is set in the command line.
		/// </summary>
		public bool IsSet { get; internal set; }

		/// <summary>
		/// Gets the number of times that this option is set in the command line.
		/// </summary>
		public int SetCount { get; internal set; }

		/// <summary>
		/// Gets the <see cref="Argument"/> instance that contains additional parameters set
		/// for this option.
		/// </summary>
		public Argument Argument { get; internal set; }

		/// <summary>
		/// Gets the value of the <see cref="Argument"/> instance for this option.
		/// </summary>
		public string Value => Argument?.Value;

		/// <summary>
		/// Sets alias names for this option.
		/// </summary>
		/// <param name="names">The alias names for this option.</param>
		/// <returns>The current <see cref="Option"/> instance.</returns>
		public Option Alias(params string[] names)
		{
			Names.AddRange(names);
			return this;
		}

		/// <summary>
		/// Marks this option as required. If a required option is not set in the command line,
		/// an exception is thrown on parsing.
		/// </summary>
		/// <returns>The current <see cref="Option"/> instance.</returns>
		public Option Required()
		{
			IsRequired = true;
			return this;
		}

		/// <summary>
		/// Marks this option as single. If a single option is set multiple times in the
		/// command line, an exception is thrown on parsing.
		/// </summary>
		/// <returns>The current <see cref="Option"/> instance.</returns>
		public Option Single()
		{
			IsSingle = true;
			return this;
		}

		/// <summary>
		/// Sets the action to invoke when the option is set.
		/// </summary>
		/// <param name="action">The action to invoke when the option is set.</param>
		/// <returns>The current <see cref="Option"/> instance.</returns>
		public Option Do(Action<Argument> action)
		{
			Action = action;
			return this;
		}
	}
}
