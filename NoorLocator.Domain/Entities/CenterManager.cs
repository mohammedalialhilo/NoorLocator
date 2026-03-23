using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class CenterManager : Entity
{
    public int UserId { get; set; }

    public int CenterId { get; set; }

    public bool Approved { get; set; }

    public User? User { get; set; }

    public Center? Center { get; set; }
}
