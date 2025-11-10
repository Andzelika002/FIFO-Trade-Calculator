using FIFOTradeCalc.Constants;
using FIFOTradeCalc.Infrastructure;
using FIFOTradeCalc.Models;
using FIFOTradeCalc.Services;
using System.Globalization;
using System.Text;

namespace FIFOTradeCalc
{
	/// <summary>
	/// Entry point for the FIFO Trade Calculator console application.
	/// Handles user input, trade loading, processing, and result output.
	/// </summary>
	class Program
	{
		private static readonly FileResultWriter _fileWriter = new FileResultWriter();

		/// <summary>
		/// Application entry point. Handles all control flow and error handling.
		/// </summary>
		/// <param name="args">Optional command-line arguments (first argument may specify the CSV file path).</param>
		static void Main(string[] args)
		{
			try
			{
				string filePath = GetFilePath(args);

				CsvReader csvReader = new CsvReader(filePath);
				CsvReadResult<Trade> csvResult = csvReader.LoadTrades();

				if (PrintCsvErrors(csvResult))
					return;

				string clientName = GetClientFromUser(csvResult.Clients);

				if (string.IsNullOrEmpty(clientName))
					return;

				DateTime? targetDate = GetDate();
				if (targetDate == null)
					return;

				TradeProcessor tradeProcessor = new TradeProcessor();
				TradeProcessResult processResult = tradeProcessor.ProcessTrades(csvResult.Data, clientName, targetDate.Value);

				WriteResults(processResult, clientName, targetDate.Value);
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Results.ErrorItem, ex.Message));
			}
			catch (InvalidDataException ex)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Results.ErrorItem, ex.Message));

			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Error.ProgramFailed, ex.Message));
			}

			Console.WriteLine(ConsoleMessages.Prompts.Exit);
			Console.ReadKey();
		}

		/// <summary>
		/// Determines the CSV file path either from command-line arguments or user input.
		/// Defaults to a predefined file path if none provided.
		/// </summary>
		private static string GetFilePath(string[] args)
		{
			if (args.Length > 0 && File.Exists(args[0]))
			{
				return args[0];
			}

			Console.Write(string.Format(ConsoleMessages.Prompts.EnterFilePath, DataFilePath.Data));
			string input = Console.ReadLine()?.Trim() ?? string.Empty;

			if (string.IsNullOrEmpty(input))
			{
				string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
				string fullPath = Path.Combine(projectRoot, DataFilePath.Data);

				return fullPath;
			}

			return input;
		}

		/// <summary>
		/// Prompts the user to select a client from the available list of clients in the CSV data.
		/// </summary>
		/// <param name="availableClients">A list of clients found in the loaded CSV file.</param>
		/// <returns>The selected client name, or an empty string if no valid selection is made.</returns>
		private static string GetClientFromUser(List<string> availableClients)
		{
			if (!availableClients.Any())
			{
				Console.WriteLine(ConsoleMessages.Error.NoClientsFound);
				return string.Empty;
			}

			Console.WriteLine(ConsoleMessages.Prompts.AvailableClientsHeader);

			foreach (string client in availableClients)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Prompts.ClientListItem, client));
			}

			Console.Write(ConsoleMessages.Prompts.EnterClientName);
			string input = Console.ReadLine()?.Trim() ?? string.Empty;

			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine(ConsoleMessages.Error.NoClientEntered);
				return string.Empty;
			}

			// Normalize both input and available clients by removing diacritics
			string normalizedInput = RemoveDiacritics(input);

			string? matchedClient = availableClients.FirstOrDefault(client =>
				RemoveDiacritics(client).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));

			if (matchedClient == null)
			{
				Console.WriteLine(string.Format(ConsoleMessages.Error.InvalidClient, input));
				return string.Empty;
			}

			return matchedClient;
		}

		/// <summary>
		/// HACKY workaround for handling Lithuanian letters (ą, č, ę, etc.) 
		/// in the Windows console when reading input. 
		/// This strips diacritics so the console input can match client names,
		/// but it alters the original characters.
		/// Not ideal for preserving proper names, only for input comparison.
		/// </summary>
		/// <param name="text">The input string which may contain diacritic characters.</param>
		/// <returns> A string with all diacritic marks removed, leaving only the base characters.</returns>
		private static string RemoveDiacritics(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			string normalizedString = text.Normalize(NormalizationForm.FormD);
			StringBuilder stringBuilder = new StringBuilder();

			foreach (char c in normalizedString)
			{
				UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
				if (unicodeCategory != UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(c);
				}
			}

			return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
		}

		/// <summary>
		/// Prompts the user to enter a target date in a specific format.
		/// If no input is provided, defaults to the current UTC date.
		/// </summary>
		/// <returns>The entered date, or null if invalid input was given.</returns>
		private static DateTime? GetDate()
		{
			Console.Write(string.Format(ConsoleMessages.Prompts.EnterDate, CsvConstants.DateFormat));
			string input = Console.ReadLine()?.Trim() ?? string.Empty;

			if (string.IsNullOrEmpty(input))
			{
				return DateTime.UtcNow.Date;
			}

			if (DateTime.TryParseExact(input, CsvConstants.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
			{
				return date;
			}

			Console.WriteLine(string.Format(ConsoleMessages.Error.InvalidDateFormat, CsvConstants.DateFormat));
			return null;
		}

		/// <summary>
		/// Displays any CSV reading or parsing errors in a structured way.
		/// </summary>
		/// <param name="result">The CSV read result containing errors and data.</param>
		private static bool PrintCsvErrors(CsvReadResult<Trade> result)
		{
			if (result.Errors.Count > 0)
			{
				foreach (CsvReadError error in result.Errors)
				{
					if (error.LineNumber > 0)
					{
						if (string.IsNullOrEmpty(error.FieldName))
						{
							Console.WriteLine(string.Format(ConsoleMessages.Formatting.ErrorLineWithoutField,
								error.LineNumber, error.ErrorMessage));
						}
						else
						{
							Console.WriteLine(string.Format(ConsoleMessages.Formatting.ErrorLineWithField,
								error.LineNumber, error.ErrorMessage, error.FieldName));
						}
					}
					else
					{
						Console.WriteLine(string.Format(ConsoleMessages.Results.ErrorItem, error.ErrorMessage));
					}
				}

				Console.WriteLine(string.Format(ConsoleMessages.Success.CompletedProcessWithErrors,
					result.Errors.Count, result.Data.Count));
				Console.WriteLine(string.Format(ConsoleMessages.Error.TotalFound, result.Errors.Count));
				return true;
			}
			else
			{
				Console.WriteLine(string.Format(ConsoleMessages.Success.CompletedProcess, result.Data.Count));
				return false;
			}
		}

		/// <summary>
		/// Writes the final FIFO calculation results to a file and prints a summary to the console.
		/// </summary>
		/// <param name="processResult">The trade processing result object containing all outputs.</param>
		/// <param name="clientName">The name of the processed client.</param>
		/// <param name="targetDate">The date up to which trades were included.</param>
		private static void WriteResults(TradeProcessResult processResult, string clientName, DateTime targetDate)
		{
			if (processResult.IsSuccess)
			{
				string filePath = _fileWriter.WriteFifoResults(processResult.FifoResults, clientName, targetDate);

				Console.WriteLine(string.Format(ConsoleMessages.Success.ResultsWritten, filePath));
			}
			else
			{
				Console.WriteLine(ConsoleMessages.Results.ProcessingFailedHeader);
				foreach (string error in processResult.Errors)
				{
					Console.WriteLine(string.Format(ConsoleMessages.Results.ErrorItem, error));
				}
			}
		}

	}
}