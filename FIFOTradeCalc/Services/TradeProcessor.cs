using FIFOTradeCalc.Constants;
using FIFOTradeCalc.Models;

namespace FIFOTradeCalc.Services
{
	/// <summary>
	/// Provides functionality for processing trade data.
	/// Filters trades by client and date and performs FIFO profit/loss calculations.
	/// </summary>
	public class TradeProcessor
	{
		private readonly FifoTradeCalculator _fifoCalculator;

		public TradeProcessor()
		{
			_fifoCalculator = new FifoTradeCalculator();
		}

		/// <summary>
		/// Filters and processes trade data for the specified client and date.
		/// Performs FIFO calculations on the provided trade data.
		/// </summary>
		/// <param name="trades">The trade data to process.</param>
		/// <param name="clientName">The client name used to filter trades.</param>
		/// <param name="targetDate">The cutoff date for including trades (inclusive).</param>
		/// <returns>
		/// A TradeProcessResult object containing all intermediate and final results,
		/// including errors if encountered during processing.
		/// </returns>
		public TradeProcessResult ProcessTrades(List<Trade> trades, string clientName, DateTime targetDate)
		{
			TradeProcessResult result = new TradeProcessResult();

			try
			{
				if (!trades.Any())
				{
					result.Errors.Add(ConsoleMessages.Error.NoTradesFound);
					return result;
				}

				List<Trade> filteredTrades = FilterTrades(trades, clientName, targetDate);
				result.FilteredTrades = filteredTrades;

				if (!filteredTrades.Any())
				{
					result.Errors.Add(string.Format(ConsoleMessages.Error.ClientNotFound, clientName, targetDate));
					return result;
				}

				result.FifoResults = _fifoCalculator.CalculateFifo(filteredTrades);
				result.IsSuccess = true;
			}
			catch (Exception ex)
			{
				result.Errors.Add(string.Format(ConsoleMessages.Error.ProcessingFailed, ex.Message));
			}

			return result;
		}

		/// <summary>
		/// Filters the provided list of trades based on the client name and cutoff date.
		/// </summary>
		/// <param name="trades">The list of trades to filter.</param>
		/// <param name="clientName">The client name used for filtering.</param>
		/// <param name="targetDate">The cutoff date for including trades.</param>
		/// <returns>
		/// A list of trades belonging to the specified client that occurred on or before the given date,
		/// ordered by date and trade ID.
		/// </returns>
		private List<Trade> FilterTrades(List<Trade> trades, string clientName, DateTime targetDate)
		{
			return trades
				.Where(t => t.Client?.Equals(clientName, StringComparison.OrdinalIgnoreCase) == true)
				.Where(t => t.Date <= targetDate)
				.OrderBy(t => t.Date)
				.ThenBy(t => t.TradeId)
				.ToList();
		}
	}
}
