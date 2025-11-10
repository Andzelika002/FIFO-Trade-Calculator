using FIFOTradeCalc.Infrastructure;

namespace FIFOTradeCalc.Models
{
	/// <summary>
	/// Result object containing trade processing information
	/// </summary>
	public class TradeProcessResult
	{
		public bool IsSuccess { get; set; }
		public CsvReadResult<Trade> CsvReadResult { get; set; }
		public List<Trade> FilteredTrades { get; set; } = new List<Trade>();
		public List<FifoResult> FifoResults { get; set; } = new List<FifoResult>();
		public List<string> Errors { get; set; } = new List<string>();
		public bool HasErrors => Errors.Any();
	}
}
