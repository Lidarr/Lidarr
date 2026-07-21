using System;
using System.Collections.Concurrent;
using System.Threading;

namespace NzbDrone.Core.MediaFiles.TrackImport.Manual
{
    public interface IManualImportProgressService
    {
        string CurrentId { get; }
        ManualImportProgress Get(string id);
        void Begin(string id, string message);
        void Report(string message, double percent);
        void ReportRange(string message, int completed, int total, double startPercent, double endPercent);
        void Complete(string message);
        void Fail(string message);
        void ClearCurrent();
    }

    public class ManualImportProgress
    {
        public string Id { get; set; }
        public double Percent { get; set; }
        public string Message { get; set; }
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ManualImportProgressService : IManualImportProgressService
    {
        private static readonly AsyncLocal<string> CurrentProgressId = new AsyncLocal<string>();
        private readonly ConcurrentDictionary<string, ManualImportProgress> _progress = new ConcurrentDictionary<string, ManualImportProgress>();

        public string CurrentId => CurrentProgressId.Value;

        public ManualImportProgress Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return _progress.TryGetValue(id, out var progress) ? progress : null;
        }

        public void Begin(string id, string message)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            CurrentProgressId.Value = id;
            _progress[id] = NewProgress(id, 0.0, message, false, false);
        }

        public void Report(string message, double percent)
        {
            var id = CurrentId;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            _progress[id] = NewProgress(id, Clamp(percent), message, false, false);
        }

        public void ReportRange(string message, int completed, int total, double startPercent, double endPercent)
        {
            total = Math.Max(1, total);
            completed = Math.Max(0, Math.Min(total, completed));
            var span = Math.Max(0.0, endPercent - startPercent);
            Report(message, startPercent + (span * completed / total));
        }

        public void Complete(string message)
        {
            var id = CurrentId;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            _progress[id] = NewProgress(id, 100.0, message, true, false);
        }

        public void Fail(string message)
        {
            var id = CurrentId;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            var previous = Get(id);
            _progress[id] = NewProgress(id, previous?.Percent ?? 0.0, message, true, true);
        }

        public void ClearCurrent()
        {
            CurrentProgressId.Value = null;
        }

        private static ManualImportProgress NewProgress(string id, double percent, string message, bool isComplete, bool hasError)
        {
            return new ManualImportProgress
            {
                Id = id,
                Percent = Math.Round(Clamp(percent), 1),
                Message = message,
                IsComplete = isComplete,
                HasError = hasError,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static double Clamp(double value)
        {
            return Math.Max(0.0, Math.Min(100.0, value));
        }
    }
}
