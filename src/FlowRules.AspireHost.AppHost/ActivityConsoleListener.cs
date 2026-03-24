using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FlowRules.AspireHost.AppHost;

public sealed class ActivityConsoleListener : IDisposable
{
    private readonly ActivityListener _listener;

    public ActivityConsoleListener(Func<ActivitySource, bool>? shouldListenTo = null)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = shouldListenTo ?? (_ => true),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => Console.WriteLine($"[Activity Start] {activity.Source.Name} - {activity.DisplayName}"),
            ActivityStopped = activity =>
            {
                Console.WriteLine($"[Activity Stop] {activity.Source.Name} - {activity.DisplayName} - Duration:{activity.Duration}");
                foreach (KeyValuePair<string, string?> tag in activity.Tags)
                {
                    Console.WriteLine($"  {tag.Key}: {tag.Value}");
                }
            }
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
