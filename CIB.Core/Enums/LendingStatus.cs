namespace CIB.Core.Enums
{
    public enum LendingStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending,
        /// <summary>
        /// Processed
        /// </summary>
        Processed,
        /// <summary>
        /// Failed
        /// </summary>
        Failed,
				/// <summary>
				/// Failed
				/// </summary>
				Disburse,
			}

		public enum LendingAction
		{
			/// <summary>
			/// Payment_Disbursment
			/// </summary>
			Disbursment,
			/// <summary>
			/// Processed
			/// </summary>
			Repayment_Date_Extension,
			/// <summary>
			/// Failed
			/// </summary>
			Pre_Liquidation,
			/// <summary>
			/// Failed
			/// </summary>
			Liquidation,
		}
}