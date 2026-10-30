using System;
using System.Collections.Generic;
using System.Globalization;

namespace EmployeeAttendenceManagmentSystem.Helpers
{
    public static class DobHelper
    {
        // Formats a date of birth may be typed or stored in.
        // Day-first and month-first are both listed, so 15/01/1985 and 01/15/1985 both work.
        private static readonly string[] Formats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy",
            "ddMMyyyy", "yyyy-MM-dd", "yyyy/MM/dd",
            "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy",
            "dd-MMM-yyyy", "d-MMM-yyyy"
        };

        // Login check: the typed password must equal the Password column of QMS.
        //  1) exactly the same text, or
        //  2) both are the same date, even if written in a different style
        //     (stored "1985-01-15", typed "15/01/1985").
        public static bool PasswordMatches(string typed, string stored)
        {
            if (string.IsNullOrWhiteSpace(typed) || string.IsNullOrWhiteSpace(stored))
                return false;

            typed = typed.Trim();
            stored = stored.Trim();

            if (string.Equals(typed, stored, StringComparison.Ordinal))
                return true;

            var typedDates = PossibleDates(typed);
            if (typedDates.Count == 0) return false;

            return typedDates.Overlaps(PossibleDates(stored));
        }

        // Every date the text could mean. A value like 05/06/1985 can be read two ways,
        // so both readings are returned and compared.
        private static HashSet<DateTime> PossibleDates(string text)
        {
            var result = new HashSet<DateTime>();

            // "15/01/1985 00:00:00" -> keep only the date part
            var datePart = text.Split(' ')[0];

            foreach (var format in Formats)
            {
                DateTime parsed;
                if (DateTime.TryParseExact(datePart, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed))
                    result.Add(parsed.Date);
            }
            return result;
        }

        // Kept for anything that still compares against a real DateTime column
        public static bool Matches(string typed, DateTime dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(typed)) return false;
            return PossibleDates(typed.Trim()).Contains(dateOfBirth.Date);
        }
    }
}