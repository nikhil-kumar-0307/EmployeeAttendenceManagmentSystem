using System;
using System.Globalization;

namespace EmployeeAttendenceManagmentSystem.Helpers
{
    public static class DobHelper
    {
        // Day-first formats are listed first; month-first are accepted too so
        // 01/15/1985 (the QMS import template format) also works.
        private static readonly string[] Formats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy",
            "ddMMyyyy", "yyyy-MM-dd", "yyyy/MM/dd",
            "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy"
        };

        // True if the typed text, read in ANY accepted format, equals the stored date of birth.
        // Checking every format avoids day/month ambiguity (e.g. 05/06/1985).
        public static bool Matches(string typed, DateTime dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(typed)) return false;
            typed = typed.Trim();

            foreach (var format in Formats)
            {
                DateTime parsed;
                if (DateTime.TryParseExact(typed, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed) && parsed.Date == dateOfBirth.Date)
                    return true;
            }
            return false;
        }
    }
}