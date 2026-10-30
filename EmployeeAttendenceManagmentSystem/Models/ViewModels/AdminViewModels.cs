using System.Collections.Generic;

namespace EmployeeAttendenceManagmentSystem.Models.ViewModels
{
    public class AdminHomeVm
    {
        public string DisplayName { get; set; }
        public int ManagerCount { get; set; }
        public int AssignedCount { get; set; }
        public int EmployeeCount { get; set; }
        public List<ManagerRow> TopManagers { get; set; } = new List<ManagerRow>();
    }

    public class EmployeeBrief
    {
        public string EmployeeNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
    }

    // A person found by the search box
    public class SearchRow : EmployeeBrief
    {
        public int TeamSize { get; set; }              // used on the manager list page
        public bool IsInThisTeam { get; set; }         // used on the manage page
        public string CurrentManagerNo { get; set; }   // set when the person already reports to someone else
        public string CurrentManagerName { get; set; }
    }

    public class ManagerRow : EmployeeBrief
    {
        public int TeamSize { get; set; }
    }

    public class AssignmentsVm
    {
        public string Query { get; set; }
        public List<SearchRow> Results { get; set; } = new List<SearchRow>();
        public List<ManagerRow> Managers { get; set; } = new List<ManagerRow>();
    }

    public class ManageTeamVm
    {
        public EmployeeBrief Manager { get; set; }
        public List<EmployeeBrief> Team { get; set; } = new List<EmployeeBrief>();
        public string Query { get; set; }
        public List<SearchRow> Results { get; set; } = new List<SearchRow>();
    }
}