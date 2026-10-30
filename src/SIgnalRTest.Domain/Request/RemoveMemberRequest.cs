namespace SIgnalRTest.Domain.Request;

public class RemoveMemberRequest
{
    public string GroupId { get; set; } = "";
    public string RequestedBy { get; set; } = "";   // must be the group admin (CreatedBy)
    public string MemberToRemove { get; set; } = "";
}
