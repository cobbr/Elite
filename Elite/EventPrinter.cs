using System;

using Covenant.API.Models;

namespace Elite
{
    public class EventPrinter
    {
        private static object _EventLock = new object();

        public bool PrintEvent(EventModel theEvent, string Context = "*")
        {
            lock (_EventLock)
            {
                if (ContextMatches(theEvent, Context))
                {
                    EliteConsole.PrintInfoLine();
                    switch (theEvent.Level)
                    {
                        case EventLevel.Highlight:
                            EliteConsole.PrintFormattedHighlightLine(theEvent.MessageHeader);
                            break;
                        case EventLevel.Info:
                            EliteConsole.PrintFormattedInfoLine(theEvent.MessageHeader);
                            break;
                        case EventLevel.Warning:
                            EliteConsole.PrintFormattedWarningLine(theEvent.MessageHeader);
                            break;
                        case EventLevel.Error:
                            EliteConsole.PrintFormattedErrorLine(theEvent.MessageHeader);
                            break;
                        default:
                            EliteConsole.PrintFormattedInfoLine(theEvent.MessageHeader);
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(theEvent.MessageBody))
                    {
                        EliteConsole.PrintInfoLine(theEvent.MessageBody);
                    }
                    return true;
                }
                return false;
            }
        }

        private static bool ContextMatches(EventModel theEvent, string Context = "*")
        {
            return theEvent.Context == "*" || Context.ToLower().Contains(theEvent.Context.ToLower());
        }
    }
}
