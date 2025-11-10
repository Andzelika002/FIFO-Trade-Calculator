using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIFOTradeCalc.Constants
{
	public static class FileOutputMessages
	{
		// Header section
		public const string HeaderTitle = "FIFO Calculation Results";
		public const string Client = "Client: {0}";
		public const string AsOfDate = "As of Date: {0:yyyy-MM-dd}";
		public const string Generated = "Generated: {0:yyyy-MM-dd HH:mm:ss}";
		public const char HeaderLineChar = '=';
		public const int HeaderLineLength = 70;

		// Profit/Loss table
		public const string ProfitLossHeader = "Security | Total P/L       |";
		public const string ProfitLossSeparator = "---------|-----------------|";
		public const string ProfitLossRow = "{0,-8} | {1,15} |";
		public const string PositiveSign = "+";
		public const string NegativeSign = "-";

		// Left Shares table
		public const string LeftSharesHeader = "\n" + "======================================================= Left Shares ========================================================";
		public const string LeftSharesColumnHeader = "Security | Amount       |  Price | Fee";
		public const string LeftSharesSeparator = "---------|--------------|--------|-----";
		public const string LeftSharesRow = "{0,-8} | {1,12} | {2,6} | {3}";

		// Summary section
		public const string TotalProfitLoss = "Total Profit/Loss: {0}";

		// General
		public const string NoResults = "No FIFO results to display.";
		public const char SectionLineChar = '-';
		public const int SectionLineLength = 70;
	}
}
