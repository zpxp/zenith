namespace SqlSharp.Exceptions
{
	[System.Serializable]
	public class SqlJoinException : System.Exception
	{
		public SqlJoinException() { }
		public SqlJoinException(string message) : base(message) { }
		public SqlJoinException(string message, System.Exception inner) : base(message, inner) { }
		protected SqlJoinException(
			 System.Runtime.Serialization.SerializationInfo info,
			 System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}