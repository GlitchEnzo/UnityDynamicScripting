public class UnityReportPrinter : Mono.CSharp.ReportPrinter
{
	public bool LogWarnings { get; set; }

	public UnityReportPrinter()
	{
		LogWarnings = false;
	}

	public UnityReportPrinter(bool logWarnings)
	{
		LogWarnings = logWarnings;
	}

	public override void Print(Mono.CSharp.AbstractMessage msg, bool showFullPath)
	{
		base.Print(msg, showFullPath);

		if (msg.IsWarning)
		{
			if (LogWarnings)
			{
				UnityEngine.Debug.LogWarning(msg.Text);
			}
		}
		else
		{
			UnityEngine.Debug.LogError(string.Format("{0} {1}", msg.Location, msg.Text));
			//UnityEngine.Debug.LogError(msg.Code + ", " + msg.Location);

			//foreach (var symbol in msg.RelatedSymbols)
			//  	UnityEngine.Debug.LogError(symbol);
		}
	}
}

