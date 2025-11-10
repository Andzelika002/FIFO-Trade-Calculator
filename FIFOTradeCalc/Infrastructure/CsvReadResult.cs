namespace FIFOTradeCalc.Infrastructure
{
	/// <summary>
	/// Represents the result of a CSV parsing operation with comprehensive status information.
	/// </summary>
	public class CsvReadResult<T>
	{
		public List<T> Data { get; set; } = new();
		public List<CsvReadError> Errors { get; set; } = new();
		public List<string> Clients { get; set; } = new();
	}
}
