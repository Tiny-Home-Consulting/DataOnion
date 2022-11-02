using DataOnion;

namespace SampleApp.Models
{
    public class TwoFactorAuthDid : DidBase
    {
        public Guid Id { get; set; }
        public string Number { get; set; }
        public TwoFactorAuthUserBase User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TwoFactorAuthUser : TwoFactorAuthUserBase
    {
        public int Id { get; set; }
        public ICollection<TwoFactorAuthDid> Dids { get; set; }
            = new List<TwoFactorAuthDid>();
    }
}