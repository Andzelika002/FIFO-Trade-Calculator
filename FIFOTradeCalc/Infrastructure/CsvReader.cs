using FIFOTradeCalc.Constants;
using FIFOTradeCalc.Models;
using System.Globalization;
using System.Text;

namespace FIFOTradeCalc.Infrastructure
{
	/// <summary>
	/// Reads and parses trade data from a CSV file, validating structure and field formats.
	/// </summary>
	public class CsvReader
	{
		private static readonly string[] RequiredColumns =
		{
			CsvConstants.TradeId,
			CsvConstants.Type,
			CsvConstants.Date,
			CsvConstants.Security,
			CsvConstants.Amount,
			CsvConstants.Price,
			CsvConstants.Fee
		};

		private readonly string _filePath;

		public CsvReader(string filePath)
		{
			_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
		}

		/// <summary>
		/// Loads and parses all trade records from the CSV file, validating data consistency and structure.
		/// </summary>
		/// <returns>A CsvReadResult containing successfully read trades and error details.</returns>
		public CsvReadResult<Trade> LoadTrades()
		{
			CsvReadResult<Trade> result = new CsvReadResult<Trade>();

			try
			{
				ValidateFile();

				List<string> lines = ReadFileLines();

				if (lines.Count < 2)
				{
					CsvReadError error = new CsvReadError(0, "", ConsoleMessages.Error.InsufficientRows);
					result.Errors.Add(error);
					return result;
				}

				List<string> headers = ParseHeaders(lines[0]);
				Dictionary<string, int> indexMap = CreateColumnIndexMap(headers);

				var validationResult = ValidateRequiredColumns(indexMap);
				if (!validationResult.IsValid)
				{
					CsvReadError error = new CsvReadError(1, lines[0], validationResult.ErrorMessage);
					result.Errors.Add(error);
					return result;
				}

				ProcessDataRows(lines, indexMap, result);
				result.Clients = ExtractClients(result.Data);
			}
			catch (Exception ex)
			{
				CsvReadError error = new CsvReadError(0, "", string.Format(ConsoleMessages.Error.ProcessFailed, ex.Message));
				result.Errors.Add(error);
			}

			return result;

		}

		/// <summary>
		/// Validates the existence and content of the specified CSV file.
		/// </summary>
		/// <exception cref="FileNotFoundException">Thrown if the file is missing.</exception>
		/// <exception cref="InvalidDataException">Thrown if the file is empty.</exception>
		private void ValidateFile()
		{
			if (!File.Exists(_filePath))
				throw new FileNotFoundException(string.Format(ConsoleMessages.Error.FileNotFound, _filePath));

			FileInfo fileInfo = new FileInfo(_filePath);
			if (fileInfo.Length == 0)
				throw new InvalidDataException(ConsoleMessages.Error.FileEmpty);
		}

		/// <summary>
		/// Reads all non-empty lines from the CSV file using UTF-8 encoding.
		/// </summary>
		/// <returns>A list of raw CSV lines.</returns>
		private List<string> ReadFileLines()
		{
			List<string> lines = new List<string>();

			using (FileStream stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
						lines.Add(line);
				}
			}

			return lines;
		}

		/// <summary>
		/// Parses and normalizes the header row to lowercase column names.
		/// </summary>
		/// <param name="headerLine">The first line of the CSV file containing headers.</param>
		/// <returns>A list of normalized header names.</returns>
		private List<string> ParseHeaders(string headerLine)
		{
			return headerLine.Split(CsvConstants.Delimiter)
				.Select(h => h.Trim().ToLowerInvariant())
				.ToList();
		}

		/// <summary>
		/// Builds a column index map from header names to their position in the CSV.
		/// </summary>
		/// <param name="headers">List of header names.</param>
		/// <returns>A dictionary mapping header names to column indexes.</returns>
		private Dictionary<string, int> CreateColumnIndexMap(List<string> headers)
		{
			Dictionary<string, int> indexMap = new Dictionary<string, int>();

			for (int i = 0; i < headers.Count; i++)
			{
				indexMap[headers[i]] = i;
			}

			return indexMap;
		}

		/// <summary>
		/// Ensures all required columns are present in the CSV header.
		/// </summary>
		/// <param name="indexMap">The column index mapping built from headers.</param>
		/// <returns>Validation result indicating whether required columns exist.</returns>
		private (bool IsValid, string ErrorMessage) ValidateRequiredColumns(Dictionary<string, int> indexMap)
		{
			List<string> missingColumns = RequiredColumns.Where(col => !indexMap.ContainsKey(col.ToLowerInvariant())).ToList();

			if (missingColumns.Any())
			{
				string errorMessage = missingColumns.Count > 1
					? string.Format(ConsoleMessages.Error.MissingRequiredColumns, string.Join(", ", missingColumns))
					: string.Format(ConsoleMessages.Error.MissingColumn, missingColumns[0]);

				return (false, errorMessage);
			}

			return (true, string.Empty);

		}

		/// <summary>
		/// Processes all data rows, converting each valid line into a Trade object.
		/// </summary>
		/// <param name="lines">All CSV lines including the header.</param>
		/// <param name="indexMap">Column name-to-index mapping.</param>
		/// <param name="result">Parsing result with data and errors.</param>
		private void ProcessDataRows(List<string> lines, Dictionary<string, int> indexMap, CsvReadResult<Trade> result)
		{
			for (int i = 1; i < lines.Count; i++)
			{
				ProcessSingleRow(lines[i], i + 1, indexMap, result);
			}
		}

		/// <summary>
		/// Parses a single CSV row into a trade record, collecting any field-level validation errors.
		/// </summary>
		/// <param name="line">The raw CSV line.</param>
		/// <param name="lineNumber">The current line number in the file.</param>
		/// <param name="indexMap">Column index mapping.</param>
		/// <param name="result">Aggregate CSV parsing result.</param>
		private void ProcessSingleRow(string line, int lineNumber, Dictionary<string, int> indexMap, CsvReadResult<Trade> result)
		{
			try
			{
				string[] parts = line.Split(CsvConstants.Delimiter);

				if (parts.Length != indexMap.Count)
				{
					CsvReadError error = new CsvReadError(lineNumber, line,
						string.Format(ConsoleMessages.Error.ColumnMismatch, indexMap.Count, parts.Length), "CSV Structure");
					result.Errors.Add(error);
					return;
				}

				Trade trade = CreateTradeFromRow(parts, indexMap, lineNumber, line, out var errors);

				if (errors.Any())
				{
					result.Errors.AddRange(errors);
				}
				else
				{
					result.Data.Add(trade);
				}
			}
			catch (Exception ex)
			{
				CsvReadError error = new CsvReadError(lineNumber, line, string.Format(ConsoleMessages.Error.Unexpected, ex.Message));
				result.Errors.Add(error);
			}
		}

		/// <summary>
		/// Creates Trade instance from parsed row data and handles individual field parsing errors.
		/// </summary>
		/// <param name="parts">Array of CSV field values.</param>
		/// <param name="indexMap">Column index mapping.</param>
		/// <param name="lineNumber">The current line number in the file.</param>
		/// <param name="line">The raw line content.</param>
		/// <param name="errors">Collection of parsing errors found in this row</param>
		/// <returns>A populated Trade object or a partially filled one if errors occurred.</returns>
		private Trade CreateTradeFromRow(string[] parts, Dictionary<string, int> indexMap, int lineNumber, string line, out List<CsvReadError> errors)
		{
			errors = new List<CsvReadError>();
			Trade trade = new Trade();

			try
			{
				CultureInfo lithuanianCulture = CultureInfo.GetCultureInfo("lt-LT");

				trade.TradeId = ParseField(parts, indexMap, CsvConstants.TradeId, int.Parse, lineNumber, line, errors);
				trade.Type = ParseField(parts, indexMap, CsvConstants.Type, x => x, lineNumber, line, errors);
				trade.Date = ParseField(parts, indexMap, CsvConstants.Date,
					x => DateTime.ParseExact(x, CsvConstants.DateFormat, CultureInfo.InvariantCulture),
					lineNumber, line, errors);
				trade.Client = ParseField(parts, indexMap, CsvConstants.Client, x => x, lineNumber, line, errors);
				trade.Security = ParseField(parts, indexMap, CsvConstants.Security, x => x, lineNumber, line, errors);
				trade.Amount = ParseField(parts, indexMap, CsvConstants.Amount, int.Parse, lineNumber, line, errors);
				trade.Price = ParseField(parts, indexMap, CsvConstants.Price,
					x => decimal.Parse(x, NumberStyles.Number, lithuanianCulture),
					lineNumber, line, errors);
				trade.Fee = ParseField(parts, indexMap, CsvConstants.Fee,
					x => decimal.Parse(x, NumberStyles.Currency, lithuanianCulture),
					lineNumber, line, errors);
			}
			catch (Exception ex)
			{
				errors.Add(new CsvReadError(lineNumber, line, string.Format(ConsoleMessages.Error.CreateTrade, ex.Message)));
			}

			return trade;
		}

		/// <summary>
		/// Parses and validates a single field value using the specified parser.
		/// </summary>
		/// <typeparam name="T">The target type to parse the field into.</typeparam>
		/// <param name="parts">The split CSV row values.</param>
		/// <param name="indexMap">Column index mapping.</param>
		/// <param name="fieldName">The name of the field to parse.</param>
		/// <param name="parser">Function to convert string to T.</param>
		/// <param name="lineNumber">The current line number in the file.</param>
		/// <param name="rawLine">Raw CSV line text.</param>
		/// <param name="errors">Collection of errors to append parsing failures to.</param>
		/// <returns>The parsed field value, or default if an error occurs.</returns>
		private T ParseField<T>(string[] parts, Dictionary<string, int> indexMap, string fieldName,
			Func<string, T> parser, int lineNumber, string rawLine, List<CsvReadError> errors)
		{
			if (!indexMap.TryGetValue(fieldName, out int index))
			{
				errors.Add(new CsvReadError(lineNumber, rawLine, ConsoleMessages.Error.MissingColumn, fieldName));
				return default(T);
			}

			var value = parts[index].Trim();

			if (string.IsNullOrWhiteSpace(value))
			{
				errors.Add(new CsvReadError(lineNumber, rawLine, ConsoleMessages.Error.EmptyField, fieldName));
				return default(T);
			}

			try
			{
				return parser(value);
			}
			catch (Exception ex)
			{
				errors.Add(new CsvReadError(lineNumber, rawLine, string.Format(ConsoleMessages.Error.InvalidFormat, fieldName, ex.Message), fieldName));
				return default(T);
			}
		}

		/// <summary>
		/// Extracts unique client names from trade data.
		/// </summary>
		/// <param name="trades">List of trades.</param>
		/// <returns>Sorted list of unique client names.</returns>
		private List<string> ExtractClients(List<Trade> trades)
		{
			return trades
				.Where(t => !string.IsNullOrEmpty(t.Client))
				.Select(t => t.Client)
				.Distinct()
				.OrderBy(client => client, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}
}
