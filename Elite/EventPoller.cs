// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

namespace Elite
{
    public class EventPoller
    {
        public event EventHandler<EventOccurredArgs> EventOccurred;

        public void Poll(CovenantAPI CovenantClient, int DelayMilliSeconds = 100)
        {
            DateTime toDate = DateTime.FromBinary(CovenantClient.ApiEventsTimeGet().Value);
            DateTime fromDate;
            bool done = false;
            while(!done)
            {
				fromDate = toDate;
				toDate = DateTime.FromBinary(CovenantClient.ApiEventsTimeGet().Value);
				IList<EventModel> events = CovenantClient.ApiEventsRangeByFromdateByTodateGet(fromDate.ToBinary(), toDate.ToBinary());
                foreach (var anEvent in events)
                {
                    EventOccurred?.Invoke(this, new EventOccurredArgs(anEvent));
                }
				Thread.Sleep(DelayMilliSeconds);
            }
        }
    }

    public class EventOccurredArgs : EventArgs
    {
        public EventModel theEvent { get; }

        public EventOccurredArgs(EventModel theEvent)
        {
            this.theEvent = theEvent;
        }
    }
}
