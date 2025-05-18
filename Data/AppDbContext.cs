using Microsoft.EntityFrameworkCore;

using Samples.HelloBlazorServer.Models;


namespace Samples.HelloBlazorServer.Data;

public class AppDbContext : DbContext
{
    public DbSet<TimerRecord> TimerRecords { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}