using System;

namespace Zenith
{
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