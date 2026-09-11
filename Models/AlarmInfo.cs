using DiplomWork_Ivan_2026.Enums;

namespace DiplomWork_Ivan_2026.Models
{
    public class AlarmInfo
    {
        public AlarmType Type { get; set; }
        public AlarmSeverity Severity { get; set; }
        public string Message { get; set; } = "";
        public string MessageBulgarian { get; set; } = "";
        public string RecommendedAction { get; set; } = "";
        public string RecommendedActionBulgarian { get; set; } = "";
        public DateTime Time { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public string LocalizedMessage =>
            Services.LocalizationService.IsBulgarian &&
            !string.IsNullOrWhiteSpace(MessageBulgarian)
                ? MessageBulgarian
                : Message;

        public string LocalizedRecommendedAction =>
            Services.LocalizationService.IsBulgarian &&
            !string.IsNullOrWhiteSpace(RecommendedActionBulgarian)
                ? RecommendedActionBulgarian
                : RecommendedAction;
    }
}
