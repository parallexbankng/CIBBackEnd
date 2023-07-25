using System.ComponentModel;

namespace CIB.Core.Modules.OnLending.Liquidation.Enum
{
  public enum LiquidationType
  {
    /// <summary>
    /// Full_Pre_Liquidation
    /// </summary>
    [Description("Full_Pre-Liquidation")]
    Full_Pre_Liquidation,
    /// <summary>
    /// Partial_Pre_Liquidation
    /// </summary>
    [Description("Partial_Pre-Liquidation")]
    Partial_Pre_Liquidation
  }

  public enum LiquidationDateType
  {
    /// <summary>
    /// Full_Pre_Liquidation
    /// </summary>
    [Description("30Days")]
    ThirtyDays,
    /// <summary>
    /// Partial_Pre_Liquidation
    /// </summary>
    [Description("60Days")]
    SixtyDays
  }
}