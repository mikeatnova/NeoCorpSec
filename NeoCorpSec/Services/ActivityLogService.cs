using NeoCorpSec.Models.Reporting;

namespace NeoCorpSec.Services
{
    public class ActivityLogService
    {
        public ActivityLog PrepareActivityLog(ActivityLog currentActivityLog, string action)
        {
            // Adding DateTime and Action to ActivityLog
            currentActivityLog.ActionTime = DateTime.UtcNow;
            currentActivityLog.Action = action;
            return currentActivityLog;
        }
    }
}
