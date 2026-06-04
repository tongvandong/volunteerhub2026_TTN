using BaseCore.Entities;
using System;
using System.Threading.Tasks;

namespace BaseCore.Repository.EFCore
{
    public interface IUnitOfWork : IDisposable
    {
        IEventRepositoryEF Events { get; }
        IRegistrationRepositoryEF Registrations { get; }
        ICertificateRepositoryEF Certificates { get; }
        IChannelRepositoryEF Channels { get; }
        IUserRepositoryEF Users { get; }
        IRepository<AccessToken> AccessTokens { get; }
        IRepository<AuditLog> AuditLogs { get; }
        IRepository<AuthRefreshToken> AuthRefreshTokens { get; }
        IRepository<Badge> Badges { get; }
        IRepository<CertificateJob> CertificateJobs { get; }
        IRepository<Comment> Comments { get; }
        IRepository<EventCategory> EventCategories { get; }
        IRepository<EventSponsor> EventSponsors { get; }
        IRepository<Function> Functions { get; }
        IRepository<GroupRoleAgency> GroupRoleAgencies { get; }
        IRepository<IndividualDonation> IndividualDonations { get; }
        IRepository<Like> Likes { get; }
        IRepository<Mention> Mentions { get; }
        IRepository<Module> Modules { get; }
        IRepository<ModuleFunction> ModuleFunctions { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<OrganizerVerification> OrganizerVerifications { get; }
        IRepository<Poll> Polls { get; }
        IRepository<PollVote> PollVotes { get; }
        IRepository<Post> Posts { get; }
        IRepository<Rating> Ratings { get; }
        IRepository<Role> Roles { get; }
        IRepository<RoleModuleFunction> RoleModuleFunctions { get; }
        IRepository<SeedConfiguration> SeedConfigurations { get; }
        IRepository<Setting> Settings { get; }
        IRepository<Skill> Skills { get; }
        IRepository<SponsorProfile> SponsorProfiles { get; }
        IRepository<SponsorshipProposal> SponsorshipProposals { get; }
        IRepository<SupportCampaign> SupportCampaigns { get; }
        IRepository<UserBadge> UserBadges { get; }
        IRepository<UserModule> UserModules { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<VolunteerProfile> VolunteerProfiles { get; }
        IRepository<VolunteerSkill> VolunteerSkills { get; }
        IRepository<WorkShift> WorkShifts { get; }

        Task<int> SaveChangesAsync();

        // Transaction management
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        Task<bool> CanConnectAsync();
    }
}

