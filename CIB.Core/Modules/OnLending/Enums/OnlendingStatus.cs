using System;
namespace CIB.Core.Modules.OnLending.Enums
{
	public enum OnlendingStatus
	{
		Pending = 0,
		Process,
		Active,
		Extended,
		PartialLiquidation,
		Liquidated,
		Failed
	}
}

