﻿
using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.ServiceBus.Providers
{
    /// <summary>
    /// Default EventHub receiver monitor that tracks metrics using loggers PKI support.
    /// </summary>
    public class DefaultEventHubReceiverMonitor : DefaultQueueAdapterReceiverMonitor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dimensions">Aggregation Dimension bag for EventhubReceiverMonitor</param>
        /// <param name="telemetryClient"></param>
        public DefaultEventHubReceiverMonitor(EventHubReceiverMonitorDimensions dimensions, ITelemetryClient telemetryClient)
            :base(telemetryClient)
        {
            this.LogProperties = new Dictionary<string, string>
            {
                {"Path", dimensions.EventHubPath},
                {"Partition", dimensions.EventHubPartition}
            };
        }
    }

}
