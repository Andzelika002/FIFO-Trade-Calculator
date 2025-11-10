using FIFOTradeCalc.Constants;
using FIFOTradeCalc.Models;

namespace FIFOTradeCalc.Services
{
	/// <summary>
	/// Provides FIFO (First-In-First-Out) calculation logic for a set of trade records.
	/// Matches SELL trades against earlier BUY trades to compute profit/loss per security.
	/// </summary>
	public class FifoTradeCalculator
	{
		/// <summary>
		/// Calculates FIFO-based profit/loss results for all securities in the given list of trades.
		/// </summary>
		/// <param name="trades">A list of trade entries to process.</param>
		/// <returns>List of FifoResult containing aggregated results per security.</returns>
		public List<FifoResult> CalculateFifo(List<Trade> trades)
		{
			List<FifoResult> results = new List<FifoResult>();

			if (trades == null || !trades.Any())
				return results;

			if (trades.Any(t => t.Amount < 0))
				throw new ArgumentException("Trade amount cannot be negative");
			if (trades.Any(t => t.Price < 0))
				throw new ArgumentException("Trade price cannot be negative");
			if (trades.Any(t => t.Fee < 0))
				throw new ArgumentException("Trade fee cannot be negative");

			List<IGrouping<string, Trade>> tradesBySecurity = trades
				.Where(t => !string.IsNullOrWhiteSpace(t.Security))
				.GroupBy(t => t.Security)
				.ToList();

			foreach (IGrouping<string, Trade> securityGroup in tradesBySecurity)
			{
				FifoResult securityResults = CalculateFifoSecurity(securityGroup.Key, securityGroup.ToList());
				results.Add(securityResults);
			}

			return results.OrderBy(r => r.Security).ToList();
		}

		/// <summary>
		/// Calculates FIFO profit/loss for a single security.
		/// </summary>
		/// <param name="security">Securyti name for trades group.</param>
		/// <param name="trades">Grouped trades by security.</param>
		/// <returns>Aggregated FifoResult containing profit/loss for trades group.</returns>
		private FifoResult CalculateFifoSecurity(string security, List<Trade> trades)
		{
			if (trades == null || trades.Count == 0)
				return new FifoResult { Security = security };

			List<Trade> buys = trades.Where(IsBuyTrade)
				.OrderBy(t => t.Date)
				.ThenBy(t => t.TradeId)
				.ToList();

			List<Trade> sells = trades.Where(IsSellTrade)
				.OrderBy(t => t.Date)
				.ThenBy(t => t.TradeId)
				.ToList();

			if (!sells.Any())
			{
				// No sells = no realized profit/loss
				return new FifoResult
				{
					Security = security,
					TotalProfitLoss = 0,
					LeftShares = buys
				};
			}

			decimal totalProfitLoss = 0;

			// Clone buys to track remaining amounts
			List<Trade> availableBuys = buys.Select(buy => new Trade
			{
				TradeId = buy.TradeId,
				Type = buy.Type,
				Date = buy.Date,
				Client = buy.Client,
				Security = buy.Security,
				Amount = buy.Amount,
				Price = buy.Price,
				Fee = buy.Fee
			}).ToList();


			foreach (Trade sell in sells)
			{
				List<Trade> fifoBuys = GetFifoBuysForSell(sell, availableBuys);

				var validation = ValidateSell(sell, fifoBuys);
				if (!validation.IsValid)
				{
					Console.WriteLine($"Error: {validation.ErrorMessage}");
					continue;
				}

				totalProfitLoss += ProcessSell(sell, fifoBuys);
			}

			return new FifoResult
			{
				Security = security,
				TotalProfitLoss = totalProfitLoss,
				LeftShares = availableBuys
					.Where(b => b.Amount > 0)
					.ToList()
			};
		}

		/// <summary>
		/// Determines if a trade is a BUY.
		/// </summary>
		private bool IsBuyTrade(Trade trade)
		{
			return trade.Type?.Equals(CsvConstants.Buy, StringComparison.OrdinalIgnoreCase) == true;
		}

		/// <summary>
		/// Determines if a trade is a SELL.
		/// </summary>
		private bool IsSellTrade(Trade trade)
		{
			return trade.Type?.Equals(CsvConstants.Sell, StringComparison.OrdinalIgnoreCase) == true;
		}

		/// <summary>
		/// Gets FIFO-ordered buys for a specific sell trade.
		/// </summary>
		private List<Trade> GetFifoBuysForSell(Trade sell, List<Trade> availableBuys)
		{
			return availableBuys
				.Where(b => b.Date <= sell.Date && b.Amount > 0)
				.OrderBy(b => b.Date)
				.ThenBy(b => b.TradeId)
				.ToList();
		}

		/// <summary>
		/// Ensures that enough shares exist before processing a SELL trade.
		/// </summary>
		/// <param name="sell">SELL trade to validate.</param>
		/// <param name="availableBuys">FIFO-ordered BUY trades for this sell.</param>
		private (bool IsValid, string ErrorMessage) ValidateSell(Trade sell, List<Trade> fifoBuys)
		{
			int ownedBeforeSell = fifoBuys.Sum(b => b.Amount);

			if (ownedBeforeSell < sell.Amount)
			{
				return (false,
					$"Insufficient shares to sell {sell.Amount} of {sell.Security} on {sell.Date:yyyy-MM-dd}. " +
					$"Owned: {ownedBeforeSell}. TradeId: {sell.TradeId}");
			}

			return (true, string.Empty);
		}

		/// <summary>
		/// Processes one SELL trade, matching it to BUY trades in FIFO order and calculating realized Profit/Loss.
		/// </summary>
		/// <param name="sell">SELL trade to analize.</param>
		/// <param name="fifoBuys">FIFO-ordered BUY trades for this sell.</param>
		private decimal ProcessSell(Trade sell, List<Trade> fifoBuys)
		{
			decimal sellProfitLoss = 0;
			int remainingToSell = sell.Amount;

			foreach (Trade buy in fifoBuys)
			{
				if (remainingToSell <= 0)
					break;

				int sharesToUse = Math.Min(remainingToSell, buy.Amount);

				decimal buyFeePerShare = buy.Amount > 0 ? buy.Fee / buy.Amount : 0;
				decimal sellFeePerShare = sell.Amount > 0 ? sell.Fee / sell.Amount : 0;

				// (price * shares + proportional fee)
				decimal totalBuyCost = (buy.Price * sharesToUse) + (buyFeePerShare * sharesToUse);

				// (price * shares - proportional fee)  
				decimal totalSellRevenue = (sell.Price * sharesToUse) - (sellFeePerShare * sharesToUse);

				// Calculate profit/loss for this batch
				sellProfitLoss += totalSellRevenue - totalBuyCost;
				remainingToSell -= sharesToUse;
				buy.Amount -= sharesToUse;
				buy.Fee -= buyFeePerShare * sharesToUse;
			}

			return sellProfitLoss;
		}
	}
}