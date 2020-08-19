namespace Zenith.Exceptions
{
	[System.Serializable]
	public class GenerateInsertException : System.Exception
	{
		public GenerateInsertException() { }
		public GenerateInsertException(string message) : base(message) { }
		public GenerateInsertException(string message, System.Exception inner) : base(message, inner) { }
		protected GenerateInsertException(
			 System.Runtime.Serialization.SerializationInfo info,
			 System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}