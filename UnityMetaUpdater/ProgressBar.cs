using System.Text;

/// <summary>
/// An ASCII progress bar
/// </summary>
public class ProgressBar : IProgress<double> {
	private const int blockCount = 10;

	private readonly Timer timer;

	private double currentProgress = 0;
	private string currentText = string.Empty;
	private int animationIndex = 0;

	public void Report(double value) {
		// Make sure value is in [0..1] range
		value = Math.Max(0, Math.Min(1, value));
		
		int progressBlockCount = (int) (value * blockCount);
		int percent = (int) (value * 100);
		string text =
			$"[{new string('#', progressBlockCount)}{new string('-', blockCount - progressBlockCount)}] {percent}%";
		
		UpdateText(text);
	}

	private void UpdateText(string text) {
		// Get length of common portion
		int commonPrefixLength = 0;
		int commonLength = Math.Min(currentText.Length, text.Length);
		while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength]) {
			commonPrefixLength++;
		}

		// Backtrack to the first differing character
		StringBuilder outputBuilder = new StringBuilder();
		outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

		// Output new suffix
		outputBuilder.Append(text.Substring(commonPrefixLength));

		// If the new text is shorter than the old one: delete overlapping characters
		int overlapCount = currentText.Length - text.Length;
		if (overlapCount > 0) {
			outputBuilder.Append(' ', overlapCount);
			outputBuilder.Append('\b', overlapCount);
		}

		Console.Write(outputBuilder);
		currentText = text;
	}

}