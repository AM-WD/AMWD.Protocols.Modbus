namespace AMWD.Common.Cli
{
	/// <summary>
	/// Represents a logical argument in the command line. Options with their additional
	/// parameters are combined in one argument.
	/// </summary>
	internal class Argument
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="Argument"/> class.
		/// </summary>
		/// <param name="option">The <see cref="Option"/> that is set in this argument; or null.</param>
		/// <param name="values">The additional parameter values for the option; or the argument value.</param>
		internal Argument(Option option, string[] values)
		{
			Option = option;
			Values = values;
		}

		/// <summary>
		/// Gets the <see cref="Option"/> that is set in this argument; or null.
		/// </summary>
		public Option Option { get; private set; }

		/// <summary>
		/// Gets the additional parameter values for the option; or the argument value.
		/// </summary>
		public string[] Values { get; private set; }

		/// <summary>
		/// Gets the first item of <see cref="Values"/>; or null.
		/// </summary>
		public string Value => Values.Length > 0 ? Values[0] : null;
	}
}
