namespace SqlSharp.Exceptions
{
	[System.Serializable]
	public class GenerateSelectException : System.Exception
	{
		public GenerateSelectException() { }
		public GenerateSelectException(string message) : base(message) { }
		public GenerateSelectException(string message, System.Exception inner) : base(message, inner) { }
		protected GenerateSelectException(
			 System.Runtime.Serialization.SerializationInfo info,
			 System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}