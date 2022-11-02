using DataOnion;

namespace SampleApp.Models
{
    public class TwoFactorAuthDid : IDid
    {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public IUser User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TwoFactorAuthUser : IUser
    {
        public int Id { get; set; }
        public ICollection<TwoFactorAuthDid> Dids { get; set; }
            = new List<TwoFactorAuthDid>();
    }
}