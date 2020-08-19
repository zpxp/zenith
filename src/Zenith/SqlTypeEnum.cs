using System;

namespace Zenith
{
	/// <summary>
	/// Specify type of the command to be run in <see cref="IUnitOfWork.NewCommand"/>
	/// </summary>
	[Flags]
	public enum SqlTypeEnum
	{
		Select = 1,
		Update = 1 << 1,
		Delete = 1 << 2,
		Insert = 1 << 3,
		Unknown = int.MaxValue,

	}
}