using Microsoft.EntityFrameworkCore;

using TimerTestApp.Models;


namespace TimerTestApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<TimerRecord> TimerRecords { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}