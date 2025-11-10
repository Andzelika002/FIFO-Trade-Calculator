namespace FIFOTradeCalc.Constants
{
	public static class ConsoleMessages
	{
		public static class Error
		{
			// CSV errors
			public const string FileNotFound = "Trade data file not found: {0}";
			public const string FileEmpty = "CSV file is empty";
			public const string InsufficientRows = "CSV file must contain at least a header and one data row";
			public const string MissingRequiredColumns = "Missing required columns: {0}";
			public const string MissingColumn = "Missing required column: {0}";
			public const string ColumnMismatch = "Expected {0} columns but found {1}";
			public const string EmptyField = "Required field is empty";
			public const string InvalidFormat = "Invalid format for {0}: {1}";
			public const string CreateTrade = "Failed to create trade: {0}";
			public const string Unexpected = "Unexpected error: {0}";
			public const string ProcessFailed = "Failed to process CSV file: {0}";
			public const string TotalFound = "Errors found: {0}";

			// Program-level errors
			public const string MissingFilePath = "Please provide the path to the CSV file";
			public const string NoValidData = "No valid trade data found to process";
			public const string ProgramFailed = "Application error: {0}";

			// TradeProcessor errors
			public const string CsvLoadFailed = "Failed to load trades due to CSV errors";
			public const string NoTradesFound = "No trades found to process";
			public const string ClientNotFound = "No trades found for client '{0}' up to {1:yyyy-MM-dd}";
			public const string ProcessingFailed = "Processing failed: {0}";

			// User input errors
			public const string NoClientsFound = "No clients found in the trade data";
			public const string NoClientEntered = "No client name entered";
			public const string InvalidClient = "Client '{0}' not found in the data";
			public const string InvalidDateFormat = "Invalid date format. Please use {0} format";
			
			// File operation errors
			public const string FileWriteError = "Error writing to file: {0}";
		}

		public static class Success
		{
			public const string CompletedProcess = "CSV processing completed successfully: {0} rows processed";
			public const string CompletedProcessWithErrors = "CSV processing completed with errors: {0}/{1} rows processed successfully";
			public const string ResultsWritten = "Results written to '{0}'";
		}

		public static class Prompts
		{
			public const string AvailableClientsHeader = "\nAvailable clients:";
			public const string ClientListItem = "  - {0}";
			public const string EnterClientName = "\nEnter client name: ";
			public const string EnterDate = "Enter date ({0} or press Enter for today): ";
			public const string Exit = "\nPress any key to exit...";
			public const string EnterFilePath = "Enter path to CSV file (or press Enter for {0}): ";
		}

		public static class Results
		{
			public const string ProcessingFailedHeader = "\n=== Trade Processing Failed ===";
			public const string ErrorItem = "Error: {0}";
		}

		public static class Formatting
		{
			public const string ErrorLineWithField = "Line {0}: {1} (Field: {2})";
			public const string ErrorLineWithoutField = "Line {0}: {1}";
		}
	}
}
