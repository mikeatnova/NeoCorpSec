using NeoCorpSec.Models.Reporting;

namespace NeoCorpSec.Services
{
    public class ActivityLogService
    {
        public ActivityLog PrepareActivityLog(ActivityLog currentActivityLog, string action, string activityType)
        {
            // Adding DateTime and Action to ActivityLog
            currentActivityLog.ActionTime = DateTime.UtcNow;
            currentActivityLog.Action = action;
            currentActivityLog.ActivityType = activityType;
            return currentActivityLog;
        }
    }
}
