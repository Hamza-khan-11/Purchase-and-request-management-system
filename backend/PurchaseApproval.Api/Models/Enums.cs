/// enumerations 
namespace PurchaseApproval.Api.Models;

public enum UserRole { Requester = 1, Manager = 2, ProcurementOfficer = 3, Admin = 4 }

public enum RequestStatus { Draft = 1, Submitted = 2, Approved = 3, Rejected = 4, PoCreated = 5, Cancelled = 6 }

public enum ApprovalDecision { Approved = 1, Rejected = 2 }

public enum PurchaseOrderStatus { Created = 1, Sent = 2, Delivered = 3, Cancelled = 4 }
