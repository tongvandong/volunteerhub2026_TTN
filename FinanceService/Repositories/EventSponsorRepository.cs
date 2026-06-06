using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceService.Data;
using FinanceService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceService.Repositories
{
    public interface IEventSponsorRepositoryEF
    {
        Task<List<EventSponsor>> GetByEventAsync(int eventId);
        Task<List<EventSponsor>> GetBySponsorAsync(int sponsorId);
    }

    public class EventSponsorRepositoryEF : IEventSponsorRepositoryEF
    {
        private readonly FinanceDbContext _context;

        public EventSponsorRepositoryEF(FinanceDbContext context)
        {
            _context = context;
        }

        public async Task<List<EventSponsor>> GetByEventAsync(int eventId)
        {
            return await _context.EventSponsors
                .Where(x => x.EventId == eventId)
                .ToListAsync();
        }

        public async Task<List<EventSponsor>> GetBySponsorAsync(int sponsorId)
        {
            return await _context.EventSponsors
                .Where(x => x.SponsorId == sponsorId)
                .ToListAsync();
        }
    }
}
