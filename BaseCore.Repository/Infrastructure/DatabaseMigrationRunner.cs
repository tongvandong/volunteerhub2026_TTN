using Microsoft.EntityFrameworkCore;

namespace BaseCore.Repository.Infrastructure
{
    public static class DatabaseMigrationRunner
    {
        private const string MigrationMutexName = @"Global\BaseCore_VolunteerHub_Migration";

        public static void RunWithProcessLock<TContext>(TContext dbContext, TimeSpan? timeout = null)
            where TContext : DbContext
        {
            using var mutex = new Mutex(false, MigrationMutexName);
            var acquired = false;

            try
            {
                acquired = mutex.WaitOne(timeout ?? TimeSpan.FromSeconds(30));
                if (!acquired)
                {
                    throw new TimeoutException("Timed out waiting for database migration lock.");
                }

                dbContext.Database.Migrate();
            }
            finally
            {
                if (acquired)
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
