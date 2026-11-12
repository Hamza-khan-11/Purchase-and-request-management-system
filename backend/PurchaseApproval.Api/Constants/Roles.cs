namespace PurchaseApproval.Api.Constants;

// exactly as stored in the database (UserRole enum names) bad mai mistake bhi na ho
public static class Roles
{
    public const string Requester = "Requester";
    public const string Manager = "Manager";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string Admin = "Admin";

    // Combined lists for [Authorize(Roles = ...)]

    public const string OfficerOrAdmin = ProcurementOfficer + "," + Admin;
    public const string ManagerOrAdmin = Manager + "," + Admin;
    public const string RequesterOrAdmin = Requester + "," + Admin;
    public const string ManagerOfficerOrAdmin = Manager + "," + ProcurementOfficer + "," + Admin;
}
