using Microsoft.EntityFrameworkCore;
using BaseCore.Entities;

namespace BaseCore.Repository.EFCore
{
    public interface IEventRepositoryEF : IRepository<Entities.Event>
    {
        Task<(List<Entities.Event> Items, int TotalCount)> SearchAsync(
            string? keyword, int? categoryId, string? status,
            DateTime? startDateFrom, int page, int pageSize, int? skillId = null, string? location = null, bool publicOnly = true);
        Task<List<Entities.Event>> GetByOrganizerAsync(int organizerId);
        Task<Entities.Event?> GetWithDetailsAsync(int id);
    }

    public class EventRepositoryEF : Repository<Entities.Event>, IEventRepositoryEF
    {
        public EventRepositoryEF(MySqlDbContext context) : base(context) { }

        public async Task<(List<Entities.Event> Items, int TotalCount)> SearchAsync(
            string? keyword, int? categoryId, string? status,
            DateTime? startDateFrom, int page, int pageSize, int? skillId = null, string? location = null, bool publicOnly = true)
        {
            var query = _dbSet
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(e =>
                    e.Title.ToLower().Contains(kw) ||
                    (e.Description != null && e.Description.ToLower().Contains(kw)) ||
                    (e.Location != null && e.Location.ToLower().Contains(kw)));
            }

            if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
                // For public listing: hide Approved events that have already ended
                if (status == "Approved")
                {
                    query = query.Where(e => e.EndDate > DateTime.UtcNow);
                }
            }
            else if (publicOnly)
            {
                // Mặc định public listing: chỉ Approved (chưa hết hạn) hoặc Completed.
                query = query.Where(e =>
                    (e.Status == "Approved" && e.EndDate > DateTime.UtcNow) ||
                    e.Status == "Completed");
            }

            if (startDateFrom.HasValue) query = query.Where(e => e.StartDate >= startDateFrom.Value);
            
            if (!string.IsNullOrEmpty(location))
            {
                var loc = location.ToLower();
                query = query.Where(e => e.Location != null && e.Location.ToLower().Contains(loc));
            }

            if (skillId.HasValue && skillId.Value == 0)
            {
                query = query.Where(e => e.RequiredSkillIds == null || e.RequiredSkillIds == "" || e.RequiredSkillIds == "[]");
            }

            var items = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

            if (skillId.HasValue && skillId.Value != 0)
            {
              
                items = items
                    .Where(e => RequiredSkillsContain(e.RequiredSkillIds, skillId.Value))
                    .ToList();
            }

            var totalCount = await query.CountAsync();
            items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (items, totalCount);
        }

        private static bool RequiredSkillsContain(string? requiredSkillIds, int skillId)
        {
            if (string.IsNullOrWhiteSpace(requiredSkillIds)) return false;
            try
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(requiredSkillIds);
                return ids?.Contains(skillId) == true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Entities.Event>> GetByOrganizerAsync(int organizerId)
        {
            return await _dbSet
                .Include(e => e.Category)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Entities.Event?> GetWithDetailsAsync(int id)
        {
            var ev = await _dbSet
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.WorkShifts)
                .Include(e => e.Channels)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev != null)
                ev.Channel = ev.Channels?.FirstOrDefault(c => c.ParentChannelId == null);
            return ev;
        }
    }
}
