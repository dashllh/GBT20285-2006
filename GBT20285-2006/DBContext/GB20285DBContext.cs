using System;
using System.Collections.Generic;
using GBT20285_2006.Models;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.DBContext;

public partial class GB20285DBContext : DbContext
{
    public GB20285DBContext()
    {
    }

    public GB20285DBContext(DbContextOptions<GB20285DBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Apparatus> Apparatuses { get; set; }

    public virtual DbSet<MouseWeight> MouseWeights { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:GB20285");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MouseWeight>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.TestId, e.MouseId }).HasName("PK_postexp_mouseweight");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
