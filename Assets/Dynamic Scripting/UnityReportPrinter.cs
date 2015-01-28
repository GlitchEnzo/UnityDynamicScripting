/// <summary>
/// Represents a <see cref="Mono.CSharp.ReportPrinter"/> that prints to the Unity log.
/// </summary>
public class UnityReportPrinter : Mono.CSharp.ReportPrinter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnityReportPrinter"/> class.
    /// </summary>
    public UnityReportPrinter()
    {
        LogWarnings = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityReportPrinter"/> class.
    /// </summary>
    /// <param name="logWarnings">True to log compiler warnings in addition to compiler errors.  False to only log compiler errors.</param>
    public UnityReportPrinter(bool logWarnings)
    {
        LogWarnings = logWarnings;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the report printer should log compile warnings and errors or only compile errors.
    /// </summary>
    public bool LogWarnings { get; set; }

    /// <summary>
    /// Prints the given message to the Unity log.
    /// </summary>
    /// <param name="msg">The message to print.</param>
    /// <param name="showFullPath">True to show the full path.</param>
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
        }
    }
}