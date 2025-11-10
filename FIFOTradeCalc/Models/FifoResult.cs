namespace FIFOTradeCalc.Models
{
	/// <summary>
	/// Represents the result of FIFO (First-In-First-Out) calculations for a specific security.
	/// </summary>
	public class FifoResult
	{
		public string Security { get; set; } = string.Empty;
		public decimal TotalProfitLoss { get; set; }
		public List<Trade> LeftShares { get; set; } = new List<Trade>();
	}
}
