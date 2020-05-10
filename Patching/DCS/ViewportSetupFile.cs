﻿using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Windows;
using Newtonsoft.Json;

namespace GadrocsWorkshop.Helios.Patching.DCS
{
    /// <summary>
    /// A collection of viewports that is stored to a file for each previously
    /// configured Profile that uses MonitorSetup.  Merged these files is used to
    /// collect viewports from many Profiles into one Combined Monitor Setup.
    /// </summary>
    public class ViewportSetupFile : NotificationObject
    {
        /// <summary>
        /// This string uniquely identifies the monitor layout that was active when these
        /// viewports were generated.  Using these viewports with a different monitor layout
        /// may result in invalid configurations.
        /// </summary>
        [JsonProperty("MonitorLayoutKey")]
        public string MonitorLayoutKey { get; internal set; }

        [JsonProperty("Viewports")] 
        public Dictionary<string, Rect> Viewports { get; } = new Dictionary<string, Rect>();

        internal IEnumerable<StatusReportItem> Merge(string name, ViewportSetupFile from)
        {
            if (MonitorLayoutKey != from.MonitorLayoutKey)
            {
                yield return new StatusReportItem
                {
                    Status = $"The saved viewport information from profile '{name}' does not match the current monitor layout",
                    Recommendation =
                        $"Configure DCS Monitor Setup for profile '{name}' to update the merged viewport data",
                    Severity = StatusReportItem.SeverityCode.Warning,
                    Link = StatusReportItem.ProfileEditor,
                    Flags = StatusReportItem.StatusFlags.ConfigurationUpToDate
                };
            }
            foreach (KeyValuePair<string, Rect> viewport in from.Viewports)
            {
                if (!Viewports.TryGetValue(viewport.Key, out Rect existingRect))
                {
                    // just copy it
                    Viewports.Add(viewport.Key, viewport.Value);
                    continue;
                }

                if (existingRect.Equals(viewport.Value))
                {
                    // no problem
                    continue;
                }

                // overwrite and warn
                Viewports[viewport.Key] = viewport.Value;
                yield return new StatusReportItem
                {
                    Status = $"profile '{name}' defines the viewport '{viewport.Key}' at a different screen location",
                    Recommendation =
                        $"Resolve viewport conflicts or do not include profile '{name}' in the combined monitor setup",
                    Severity = StatusReportItem.SeverityCode.Warning,
                    Link = StatusReportItem.ProfileEditor,
                    Flags = StatusReportItem.StatusFlags.ConfigurationUpToDate
                };
            }
        }

        internal void Clear()
        {
            Viewports.Clear();
        }
    }
}