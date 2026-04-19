namespace VibeCoders.Services;

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

    public class CalendarExportService : ICalendarExportService
    {
        private const int MinDurationWeeks = 1;
        private const int MaxDurationWeeks = 52;
        private const int DaysInWeek = 7;
        private const int DefaultStartHour = 10;
        private const int DefaultDurationHours = 1;

        public string GenerateCalendar(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime? startDate = null)
        {
            if (workoutTemplate == null)
            {
                throw new ArgumentNullException(nameof(workoutTemplate));
            }

            if (durationWeeks < MinDurationWeeks || durationWeeks > MaxDurationWeeks)
            {
                throw new ArgumentOutOfRangeException(nameof(durationWeeks), "Duration must be between 1 and 52 weeks.");
            }

            if (selectedDays == null || selectedDays.Length == 0)
            {
                throw new ArgumentException("At least one day must be selected.", nameof(selectedDays));
            }

            var baseDate = startDate ?? DateTime.Now;
            var icsBuilder = new StringBuilder();

            icsBuilder.AppendLine("BEGIN:VCALENDAR");
            icsBuilder.AppendLine("VERSION:2.0");
            icsBuilder.AppendLine("PRODID:-//VibeCoders//Fitness//EN");
            icsBuilder.AppendLine("CALSCALE:GREGORIAN");
            icsBuilder.AppendLine("METHOD:PUBLISH");

            var generatedEvents = GenerateWorkoutEvents(workoutTemplate, durationWeeks, selectedDays, baseDate);
            foreach (var eventContent in generatedEvents)
            {
                icsBuilder.AppendLine(eventContent);
            }

            icsBuilder.AppendLine("END:VCALENDAR");

            return icsBuilder.ToString();
        }

        public async Task<string?> SaveCalendarToDownloadsAsync(string calendarContent, string? workoutName)
        {
            if (string.IsNullOrWhiteSpace(calendarContent))
            {
                return null;
            }

            try
            {
                string downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                Directory.CreateDirectory(downloadsPath);

                string safeWorkoutName = BuildSafeWorkoutName(workoutName);
                string fileName = $"{safeWorkoutName}-{DateTime.Now:yyyyMMdd-HHmmss}.ics";
                string fullPath = Path.Combine(downloadsPath, fileName);

                await File.WriteAllTextAsync(fullPath, calendarContent);
                return fullPath;
            }
            catch
            {
                return null;
            }
        }

        private List<string> GenerateWorkoutEvents(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime baseDate)
        {
            var events = new List<string>();
            var selectedDaysHash = new HashSet<int>(selectedDays);

            for (int week = 0; week < durationWeeks; week++)
            {
                for (int dayOffset = 0; dayOffset < DaysInWeek; dayOffset++)
                {
                    var currentDate = baseDate.AddDays((week * DaysInWeek) + dayOffset);
                    int dayOfWeek = (int)currentDate.DayOfWeek;

                    if (!selectedDaysHash.Contains(dayOfWeek))
                    {
                        continue;
                    }

                    var eventContent = CreateVEvent(workoutTemplate, currentDate);
                    events.Add(eventContent);
                }
            }

            return events;
        }

        private string CreateVEvent(WorkoutTemplate workoutTemplate, DateTime eventDate)
        {
            var builder = new StringBuilder();

            var eventStart = eventDate.Date.AddHours(DefaultStartHour);
            var eventEnd = eventStart.AddHours(DefaultDurationHours);

            builder.AppendLine("BEGIN:VEVENT");
            builder.AppendLine($"DTSTART:{FormatIcsDateTime(eventStart)}");
            builder.AppendLine($"DTEND:{FormatIcsDateTime(eventEnd)}");
            builder.AppendLine($"SUMMARY:{EscapeIcsText(workoutTemplate.Name)}");

            var exerciseDescription = BuildExerciseDescription(workoutTemplate.GetExercises());
            builder.AppendLine($"DESCRIPTION:{EscapeIcsText(exerciseDescription)}");

            string uid = $"{workoutTemplate.Id}-{eventDate:yyyyMMdd}@vibecode.local";
            builder.AppendLine($"UID:{uid}");

            builder.AppendLine($"DTSTAMP:{FormatIcsDateTime(DateTime.UtcNow)}");

            builder.AppendLine("END:VEVENT");

            return builder.ToString();
        }

        private string BuildExerciseDescription(List<TemplateExercise> exercises)
        {
            if (exercises == null || exercises.Count == 0)
            {
                return "No exercises specified.";
            }

            var lines = new List<string>();
            foreach (var exercise in exercises)
            {
                string line = $"{exercise.Name} - {exercise.TargetSets}x{exercise.TargetReps} @ {exercise.TargetWeight}kg";
                lines.Add(line);
            }

            return string.Join("\n", lines);
        }

        private string FormatIcsDateTime(DateTime dateTime)
        {
            var utcDateTime = dateTime.ToUniversalTime();
            return utcDateTime.ToString("yyyyMMddTHHmmssZ");
        }

        private string EscapeIcsText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n");
        }

        private static string BuildSafeWorkoutName(string? workoutName)
        {
            string fallbackWorkoutName = string.IsNullOrWhiteSpace(workoutName)
                ? "Workout"
                : workoutName;

            var safeNameBuilder = new StringBuilder(fallbackWorkoutName.Length);
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int index = 0; index < fallbackWorkoutName.Length; index++)
            {
                char currentCharacter = fallbackWorkoutName[index];

                if (currentCharacter == ' ' || currentCharacter == '/' || currentCharacter == '\\')
                {
                    safeNameBuilder.Append('-');
                    continue;
                }

                bool isInvalidCharacter = false;
                for (int invalidCharacterIndex = 0; invalidCharacterIndex < invalidCharacters.Length; invalidCharacterIndex++)
                {
                    if (currentCharacter == invalidCharacters[invalidCharacterIndex])
                    {
                        isInvalidCharacter = true;
                        break;
                    }
                }

                safeNameBuilder.Append(isInvalidCharacter ? '-' : currentCharacter);
            }

            if (safeNameBuilder.Length == 0)
            {
                return "Workout";
            }

            return safeNameBuilder.ToString();
        }
    }
