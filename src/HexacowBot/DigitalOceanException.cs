namespace HexacowBot
{
	public sealed class DigitalOceanException : Exception
	{
		public DigitalOceanException()
		{
		}

		public DigitalOceanException(string message) : base(message)
		{
		}

		public DigitalOceanException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}