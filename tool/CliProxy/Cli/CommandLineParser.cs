using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMWD.Common.Cli
{
	/// <summary>
	/// Provides options and arguments parsing from command line arguments or a single string.
	/// </summary>
	internal class CommandLineParser
	{
		#region Private data

		private string[] _args;
		private List<Argument> _parsedArguments;
		private readonly List<Option> _options = [];

		#endregion Private data

		#region Configuration properties

		/// <summary>
		/// Gets or sets a value indicating whether the option names are case-sensitive.
		/// (Default: false)
		/// </summary>
		public bool IsCaseSensitive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether incomplete options can be automatically
		/// completed if there is only a single matching option.
		/// (Default: true)
		/// </summary>
		public bool AutoCompleteOptions { get; set; } = true;

		#endregion Configuration properties

		#region Custom arguments line parsing

		// Source: http://stackoverflow.com/a/23961658/143684
		/// <summary>
		/// Parses a single string into an arguments array.
		/// </summary>
		/// <param name="argsString">The string that contains the entire command line.</param>
		public static string[] ParseArgsString(string argsString)
		{
			// Collects the split argument strings
			var args = new List<string>();

			// Builds the current argument
			var currentArg = new StringBuilder();

			// Indicates whether the last character was a backslash escape character
			bool escape = false;

			// Indicates whether we're in a quoted range
			bool inQuote = false;

			// Indicates whether there were quotes in the current arguments
			bool hadQuote = false;

			// Remembers the previous character
			char prevCh = '\0';

			// Iterate all characters from the input string
			for (int i = 0; i < argsString.Length; i++)
			{
				char ch = argsString[i];
				if (ch == '\\' && !escape)
				{
					// Beginning of a backslash-escape sequence
					escape = true;
				}
				else if (ch == '\\' && escape)
				{
					// Double backslash, keep one
					currentArg.Append(ch);
					escape = false;
				}
				else if (ch == '"' && !escape)
				{
					// Toggle quoted range
					inQuote = !inQuote;
					hadQuote = true;
					if (inQuote && prevCh == '"')
					{
						// Doubled quote within a quoted range is like escaping
						currentArg.Append(ch);
					}
				}
				else if (ch == '"' && escape)
				{
					// Backslash-escaped quote, keep it
					currentArg.Append(ch);
					escape = false;
				}
				else if (char.IsWhiteSpace(ch) && !inQuote)
				{
					if (escape)
					{
						// Add pending escape char
						currentArg.Append('\\');
						escape = false;
					}
					// Accept empty arguments only if they are quoted
					if (currentArg.Length > 0 || hadQuote)
					{
						args.Add(currentArg.ToString());
					}
					// Reset for next argument
					currentArg.Clear();
					hadQuote = false;
				}
				else
				{
					if (escape)
					{
						// Add pending escape char
						currentArg.Append('\\');
						escape = false;
					}
					// Copy character from input, no special meaning
					currentArg.Append(ch);
				}
				prevCh = ch;
			}
			// Save last argument
			if (currentArg.Length > 0 || hadQuote)
			{
				args.Add(currentArg.ToString());
			}
			return [.. args];
		}

		/// <summary>
		/// Reads the command line arguments from a single string.
		/// </summary>
		/// <param name="argsString">The string that contains the entire command line.</param>
		public void ReadArgs(string argsString)
		{
			_args = ParseArgsString(argsString);
		}

		#endregion Custom arguments line parsing

		#region Options management

		/// <summary>
		/// Registers a named option without additional parameters.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name)
		{
			return RegisterOption(name, 0);
		}

		/// <summary>
		/// Registers a named option.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <param name="parameterCount">The number of additional parameters for this option.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name, int parameterCount)
		{
			var option = new Option(name, parameterCount);
			_options.Add(option);
			return option;
		}

		#endregion Options management

		#region Parsing method

		/// <summary>
		/// Parses all command line arguments.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		public void Parse(string[] args)
		{
			_args = args ?? throw new ArgumentNullException(nameof(args));
			Parse();
		}

		/// <summary>
		/// Parses all command line arguments.
		/// </summary>
		public void Parse()
		{
			// Use args of the current process if no other source was given
			if (_args == null)
			{
				_args = Environment.GetCommandLineArgs();
				if (_args.Length > 0)
				{
					// Skip myself (args[0])
					_args = _args.Skip(1).ToArray();
				}
			}

			// Clear/reset data
			_parsedArguments = [];
			foreach (var option in _options)
			{
				option.IsSet = false;
				option.SetCount = 0;
				option.Argument = null;
			}

			var comparison = IsCaseSensitive
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;
			var argumentWalker = new EnumerableWalker<string>(_args);
			bool optMode = true;
			foreach (string arg in argumentWalker.Cast<string>())
			{
				if (arg == "--")
				{
					optMode = false;
				}
				else if (optMode && (arg.StartsWith("/") || arg.StartsWith("-")))
				{
					string optName = arg.Substring(arg.StartsWith("--") ? 2 : 1);

					// Split option value if separated with : or = instead of whitespace
					int separatorIndex = optName.IndexOfAny([':', '=']);
					string optValue = null;
					if (separatorIndex != -1)
					{
						optValue = optName.Substring(separatorIndex + 1);
						optName = optName.Substring(0, separatorIndex);
					}

					// Find the option with complete name match
					var option = _options.FirstOrDefault(o => o.Names.Any(n => n.Equals(optName, comparison)));
					if (option == null)
					{
						// Try to complete the name to a unique registered option
						var matchingOptions = _options.Where(o => o.Names.Any(n => n.StartsWith(optName, comparison))).ToList();
						if (AutoCompleteOptions && matchingOptions.Count > 1)
							throw new Exception("Invalid option, completion is not unique: " + arg);

						if (!AutoCompleteOptions || matchingOptions.Count == 0)
							throw new Exception("Unknown option: " + arg);

						// Accept the single auto-completed option
						option = matchingOptions[0];
					}

					// Check for single usage
					if (option.IsSingle && option.IsSet)
						throw new Exception("Option cannot be set multiple times: " + arg);

					// Collect option values from next argument strings
					string[] values = new string[option.ParameterCount];
					for (int i = 0; i < option.ParameterCount; i++)
					{
						if (optValue != null)
						{
							// The first value was included in this argument string
							values[i] = optValue;
							optValue = null;
						}
						else
						{
							// Fetch another argument string
							values[i] = argumentWalker.GetNext();
						}

						if (values[i] == null)
							throw new Exception("Missing argument " + (i + 1) + " for option: " + arg);
					}
					var argument = new Argument(option, values);

					// Set usage data on the option instance for quick access
					option.IsSet = true;
					option.SetCount++;
					option.Argument = argument;

					if (option.Action != null)
					{
						option.Action(argument);
					}
					else
					{
						_parsedArguments.Add(argument);
					}
				}
				else
				{
					_parsedArguments.Add(new Argument(null, [arg]));
				}
			}

			var missingOption = _options.FirstOrDefault(o => o.IsRequired && !o.IsSet);
			if (missingOption != null)
				throw new Exception("Missing required option: /" + missingOption.Names[0]);
		}

		#endregion Parsing method

		#region Parsed data properties

		/// <summary>
		/// Gets the parsed arguments.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse()"/> method in advance for
		/// exception handling.
		/// </remarks>
		public Argument[] Arguments
		{
			get
			{
				if (_parsedArguments == null)
					Parse();

				return [.. _parsedArguments];
			}
		}

		/// <summary>
		/// Gets the options that are set in the command line, including their value.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse()"/> method in advance for
		/// exception handling.
		/// </remarks>
		public Option[] SetOptions
		{
			get
			{
				if (_parsedArguments == null)
					Parse();

				return _parsedArguments
					.Where(a => a.Option != null)
					.Select(a => a.Option)
					.ToArray();
			}
		}

		/// <summary>
		/// Gets the free arguments that are set in the command line and don't belong to an option.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse()"/> method in advance for
		/// exception handling.
		/// </remarks>
		public string[] FreeArguments
		{
			get
			{
				if (_parsedArguments == null)
					Parse();

				return _parsedArguments
					.Where(a => a.Option == null)
					.Select(a => a.Value)
					.ToArray();
			}
		}

		#endregion Parsed data properties
	}
}
