namespace SqlSharp.Exceptions
{
	[System.Serializable]
	public class SqlMapException : System.Exception
	{
		public SqlMapException() { }
		public SqlMapException(string message) : base(message) { }
		public SqlMapException(string message, System.Exception inner) : base(message, inner) { }
		protected SqlMapException(
			 System.Runtime.Serialization.SerializationInfo info,
			 System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}