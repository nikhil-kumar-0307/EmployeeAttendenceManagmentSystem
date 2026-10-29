using System;
using System.Collections.Generic;

namespace EmployeeAttendenceManagmentSystem.Models.ViewModels
{
    public class EmployeeHomeVm
    {
        public string DisplayName { get; set; }
        public int SubordinateCount { get; set; }
    }

    // Per employee per day, summarised from the punch log
    public class PunchSummary
    {
        public string EmpCode { get; set; }
        public DateTime? Day { get; set; }
        public DateTime First { get; set; }
        public DateTime Last { get; set; }
        public int Count { get; set; }
        public string Name { get; set; }
    }

    public class DayRow
    {
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string InText { get; set; }
        public string OutText { get; set; }
        public string HoursText { get; set; }
        public int Punches { get; set; }
        public string StatusClass { get { return (Status ?? "").ToLower().Replace(" ", ""); } }
    }

    public class MonthlyAttendanceVm
    {
        public string EmployeeNo { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime Month { get; set; }
        public bool IsSelf { get; set; }

        public List<DayRow> Rows { get; set; } = new List<DayRow>();

        public int Present { get; set; }
        public int Absent { get; set; }
        public int WeeklyOff { get; set; }
        public int NotMarked { get; set; }
    }

    public class SubordinateRow
    {
        public string EmployeeNo { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Status { get; set; }
        public string InText { get; set; }
        public string OutText { get; set; }
        public string StatusClass { get { return (Status ?? "").ToLower().Replace(" ", ""); } }
    }

    public class SubordinateDayVm
    {
        public DateTime Date { get; set; }
        public List<SubordinateRow> Rows { get; set; } = new List<SubordinateRow>();

        public int Total { get { return Rows.Count; } }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int WeeklyOff { get; set; }
        public int NotMarked { get; set; }
    }
}