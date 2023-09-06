namespace Backend.Models;

using Microsoft.EntityFrameworkCore;

public class Context : DbContext {
    public Context(DbContextOptions<Context> options)
        : base(options)
    {}

    public DbSet<ModeState> States { get; set; } = null!;
}