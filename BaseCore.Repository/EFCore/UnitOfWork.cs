using BaseCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BaseCore.Repository.EFCore
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MySqlDbContext _context;

        public IEventRepositoryEF Events { get; }
        public IRegistrationRepositoryEF Registrations { get; }
        public IChannelRepositoryEF Channels { get; }
        public ICertificateRepositoryEF Certificates { get; }
        public IUserRepositoryEF Users { get; }

        private IRepository<AccessToken>? _accessTokens;
        private IRepository<AuthRefreshToken>? _authRefreshTokens;
        private IRepository<Badge>? _badges;
        private IRepository<CertificateJob>? _certificateJobs;
        private IRepository<Comment>? _comments;
        private IRepository<EventCategory>? _eventCategories;
        private IRepository<Function>? _functions;
        private IRepository<GroupRoleAgency>? _groupRoleAgencies;
        private IRepository<Like>? _likes;
        private IRepository<Mention>? _mentions;
        private IRepository<Module>? _modules;
        private IRepository<ModuleFunction>? _moduleFunctions;
        private IRepository<Notification>? _notifications;
        private IRepository<Poll>? _polls;
        private IRepository<PollVote>? _pollVotes;
        private IRepository<Post>? _posts;
        private IRepository<Rating>? _ratings;
        private IRepository<Role>? _roles;
        private IRepository<RoleModuleFunction>? _roleModuleFunctions;
        private IRepository<SeedConfiguration>? _seedConfigurations;
        private IRepository<Setting>? _settings;
        private IRepository<Skill>? _skills;
        private IRepository<SponsorProfile>? _sponsorProfiles;
        private IRepository<UserBadge>? _userBadges;
        private IRepository<UserModule>? _userModules;
        private IRepository<UserRole>? _userRoles;
        private IRepository<VolunteerProfile>? _volunteerProfiles;
        private IRepository<SupportCampaign>? _supportCampaigns;
        private IRepository<IndividualDonation>? _individualDonations;
        private IRepository<SponsorshipProposal>? _sponsorshipProposals;
        private IRepository<EventSponsor>? _eventSponsors;
        private IRepository<AuditLog>? _auditLogs;
        private IRepository<OrganizerVerification>? _organizerVerifications;
        private IRepository<VolunteerSkill>? _volunteerSkills;
        private IRepository<WorkShift>? _workShifts;

        public UnitOfWork(
            MySqlDbContext context,
            IEventRepositoryEF eventRepository,
            IRegistrationRepositoryEF registrationRepository,
            IChannelRepositoryEF channelRepository,
            ICertificateRepositoryEF certificateRepository,
            IUserRepositoryEF userRepository)
        {
            _context = context;
            Events = eventRepository;
            Registrations = registrationRepository;
            Channels = channelRepository;
            Certificates = certificateRepository;
            Users = userRepository;
        }

        public IRepository<AccessToken> AccessTokens => _accessTokens ??= new Repository<AccessToken>(_context);
        public IRepository<AuthRefreshToken> AuthRefreshTokens => _authRefreshTokens ??= new Repository<AuthRefreshToken>(_context);
        public IRepository<Badge> Badges => _badges ??= new Repository<Badge>(_context);
        public IRepository<CertificateJob> CertificateJobs => _certificateJobs ??= new Repository<CertificateJob>(_context);
        public IRepository<Comment> Comments => _comments ??= new Repository<Comment>(_context);
        public IRepository<EventCategory> EventCategories => _eventCategories ??= new Repository<EventCategory>(_context);
        public IRepository<Function> Functions => _functions ??= new Repository<Function>(_context);
        public IRepository<GroupRoleAgency> GroupRoleAgencies => _groupRoleAgencies ??= new Repository<GroupRoleAgency>(_context);
        public IRepository<Like> Likes => _likes ??= new Repository<Like>(_context);
        public IRepository<Mention> Mentions => _mentions ??= new Repository<Mention>(_context);
        public IRepository<Module> Modules => _modules ??= new Repository<Module>(_context);
        public IRepository<ModuleFunction> ModuleFunctions => _moduleFunctions ??= new Repository<ModuleFunction>(_context);
        public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
        public IRepository<Poll> Polls => _polls ??= new Repository<Poll>(_context);
        public IRepository<PollVote> PollVotes => _pollVotes ??= new Repository<PollVote>(_context);
        public IRepository<Post> Posts => _posts ??= new Repository<Post>(_context);
        public IRepository<Rating> Ratings => _ratings ??= new Repository<Rating>(_context);
        public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
        public IRepository<RoleModuleFunction> RoleModuleFunctions => _roleModuleFunctions ??= new Repository<RoleModuleFunction>(_context);
        public IRepository<SeedConfiguration> SeedConfigurations => _seedConfigurations ??= new Repository<SeedConfiguration>(_context);
        public IRepository<Setting> Settings => _settings ??= new Repository<Setting>(_context);
        public IRepository<Skill> Skills => _skills ??= new Repository<Skill>(_context);
        public IRepository<SponsorProfile> SponsorProfiles => _sponsorProfiles ??= new Repository<SponsorProfile>(_context);
        public IRepository<UserBadge> UserBadges => _userBadges ??= new Repository<UserBadge>(_context);
        public IRepository<UserModule> UserModules => _userModules ??= new Repository<UserModule>(_context);
        public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
        public IRepository<VolunteerProfile> VolunteerProfiles => _volunteerProfiles ??= new Repository<VolunteerProfile>(_context);
        public IRepository<SupportCampaign> SupportCampaigns => _supportCampaigns ??= new Repository<SupportCampaign>(_context);
        public IRepository<IndividualDonation> IndividualDonations => _individualDonations ??= new Repository<IndividualDonation>(_context);
        public IRepository<SponsorshipProposal> SponsorshipProposals => _sponsorshipProposals ??= new Repository<SponsorshipProposal>(_context);
        public IRepository<EventSponsor> EventSponsors => _eventSponsors ??= new Repository<EventSponsor>(_context);
        public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
        public IRepository<OrganizerVerification> OrganizerVerifications => _organizerVerifications ??= new Repository<OrganizerVerification>(_context);
        public IRepository<VolunteerSkill> VolunteerSkills => _volunteerSkills ??= new Repository<VolunteerSkill>(_context);
        public IRepository<WorkShift> WorkShifts => _workShifts ??= new Repository<WorkShift>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel)
        {
            return await _context.Database.BeginTransactionAsync(isolationLevel);
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public async Task<bool> CanConnectAsync()
        {
            return await _context.Database.CanConnectAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

