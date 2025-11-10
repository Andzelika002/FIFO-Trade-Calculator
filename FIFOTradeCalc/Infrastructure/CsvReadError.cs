namespace FIFOTradeCalc.Infrastructure
{
	/// <summary>
	/// Represents an error encountered during CSV parsing with contextual information.
	/// </summary>
	public record CsvReadError(int LineNumber, string RawLine, string ErrorMessage, string FieldName = "");
}
