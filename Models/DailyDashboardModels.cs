using System;
using System.Collections.Generic;

namespace NKKSeatBangpoo.Models
{
    public class DailySummary
    {
        public int TotalTargetToday { get; set; }        // Total pieces to produce today
        public int TotalProducedToday { get; set; }      // Total pieces registered today
        public int TotalRegisteredToday { get; set; }    // Total unique tags scanned/bound today
        public int TotalPendingGate { get; set; }        // Total pieces registered but not yet in gate
        public int TotalInStockToday { get; set; }       // Total pieces passed gate today
        public int TotalRemainingProduce { get; set; }   // (TotalTarget - TotalProduced)
        public int TotalOrdersToday { get; set; }
    }

    public class SeatTypeSummary
    {
        public string SeatTypeName { get; set; } = "Unknown";
        public int OrderCount { get; set; }
        public int TargetQty { get; set; }
        public int ProducedQty { get; set; }            // (Registered)
        public int PendingGateQty { get; set; }
        public int InStockQty { get; set; }
        public int RemainingQty { get; set; }
        
        // UI Helpers
        public double ProgressPct => TargetQty > 0 ? (double)ProducedQty / TargetQty : 0;
        public string ProgressColor => ProgressPct >= 1 ? "#44DD66" : "#4488AA";
    }

    public class HourlyThroughput
    {
        public int Hour { get; set; }
        public int Count { get; set; }
        public string HourLabel => $"{Hour:D2}:00";
    }
}
