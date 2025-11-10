namespace FIFOTradeCalc.Models
{
	public class Trade
	{
		public int TradeId { get; set; }
		public string Type { get; set; } = string.Empty; // BUY or SELL
		public DateTime Date { get; set; }
		public string Client { get; set; } = string.Empty;
		public string Security { get; set; } = string.Empty;
		public int Amount { get; set; }
		public decimal Price { get; set; }
		public decimal Fee { get; set; }
	}
}
