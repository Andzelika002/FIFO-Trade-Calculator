using FIFOTradeCalc.Constants;
using FIFOTradeCalc.Models;

namespace FIFOTradeCalc.Infrastructure
{
	/// <summary>
	/// Handles writing FIFO calculation results to output files with formatted tables.
	/// </summary>
	public class FileResultWriter
	{
		/// <summary>
		/// Writes FIFO calculation results to a specified file path with formatted output.
		/// </summary>
		/// <param name="results">The FIFO calculation results to write.</param>
		/// <param name="clientName">The name of the client for the report.</param>
		/// <param name="targetDate">The target date for the calculation.</param>
		/// <param name="filePath">The output file path. Defaults to DataFilePath.Output.</param>
		public string WriteFifoResults(List<FifoResult> results, string clientName, DateTime targetDate, string filePath = DataFilePath.Output)
		{
			try
			{
				string fullPath = Path.GetFullPath(filePath);

				using (StreamWriter writer = new StreamWriter(fullPath))
				{
					WriteHeader(writer, clientName, targetDate);

					if (!results.Any())
					{
						writer.WriteLine(FileOutputMessages.NoResults);
						return fullPath;
					}

					WriteResultsTable(writer, results);
					WriteLeftSharesTable(writer, results);
				}

				return fullPath;
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Error.FileWriteError, ex.Message));
				return filePath;
			}
		}

		/// <summary>
		/// Writes the header section of the FIFO results report.
		/// </summary>
		/// <param name="writer">The stream writer to write to.</param>
		/// <param name="clientName">The name of the client.</param>
		/// <param name="targetDate">The target date for the calculation.</param>
		private void WriteHeader(StreamWriter writer, string clientName, DateTime targetDate)
		{
			writer.WriteLine(FileOutputMessages.HeaderTitle);
			writer.WriteLine(string.Format(FileOutputMessages.Client, clientName));
			writer.WriteLine(string.Format(FileOutputMessages.AsOfDate, targetDate));
			writer.WriteLine(string.Format(FileOutputMessages.Generated, DateTime.Now));
			writer.WriteLine(new string(FileOutputMessages.HeaderLineChar, FileOutputMessages.HeaderLineLength));
		}

		/// <summary>
		/// Writes the profit/loss table showing results for each security.
		/// </summary>
		/// <param name="writer">The stream writer to write to.</param>
		/// <param name="results">The FIFO calculation results.</param>
		private void WriteResultsTable(StreamWriter writer, List<FifoResult> results)
		{
			writer.WriteLine(FileOutputMessages.ProfitLossHeader);
			writer.WriteLine(FileOutputMessages.ProfitLossSeparator);

			foreach (FifoResult result in results)
			{
				string profitLossSign = result.TotalProfitLoss >= 0 ?
					FileOutputMessages.PositiveSign :
					FileOutputMessages.NegativeSign;
				string formattedAmount = $"{profitLossSign}{Math.Abs(result.TotalProfitLoss):N2}";
				writer.WriteLine(string.Format(FileOutputMessages.ProfitLossRow,
					result.Security, formattedAmount));
			}

			writer.WriteLine(new string(FileOutputMessages.SectionLineChar, FileOutputMessages.SectionLineLength));

			// Calculate and write totals
			decimal totalProfitLoss = results.Sum(r => r.TotalProfitLoss);

			string totalSign = totalProfitLoss >= 0 ?
				FileOutputMessages.PositiveSign :
				FileOutputMessages.NegativeSign;
			string totalFormatted = $"{totalSign}{Math.Abs(totalProfitLoss):N2}";

			writer.WriteLine(string.Format(FileOutputMessages.TotalProfitLoss, totalFormatted));
		}

		/// <summary>
		/// Writes the left shares table showing remaining inventory for each security.
		/// </summary>
		/// <param name="writer">The stream writer to write to.</param>
		/// <param name="results">The FIFO calculation results.</param>
		private void WriteLeftSharesTable(StreamWriter writer, List<FifoResult> results)
		{
			List<FifoResult> securitiesWithLeftShares = results.Where(r => r.LeftShares.Any()).ToList();

			if (!securitiesWithLeftShares.Any())
				return;

			writer.WriteLine(FileOutputMessages.LeftSharesHeader);
			writer.WriteLine(FileOutputMessages.LeftSharesColumnHeader);
			writer.WriteLine(FileOutputMessages.LeftSharesSeparator);

			foreach (FifoResult result in securitiesWithLeftShares)
			{
				foreach (Trade leftShare in result.LeftShares)
				{
					writer.WriteLine(string.Format(FileOutputMessages.LeftSharesRow,
						result.Security, leftShare.Amount, leftShare.Price, leftShare.Fee));
				}
			}

			writer.WriteLine(new string(FileOutputMessages.SectionLineChar, FileOutputMessages.SectionLineLength));
		}
	}
}
