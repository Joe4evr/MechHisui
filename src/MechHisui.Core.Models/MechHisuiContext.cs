using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MechHisui.FateGOLib;
using MechHisui.HisuiBets;

namespace MechHisui.Core
{
    public class MechHisuiContext : DbContext
    {
        public DbSet<ServantProfile> Servants { get; set; }
        public DbSet<ServantSkill> Skills { get; set; }
        public DbSet<ServantTrait> Traits { get; set; }
        public DbSet<CEProfile> CEs { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<MysticCode> MysticCodes { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlite();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServantProfile>(ent =>
            {
                ent.HasKey(p => p.Id);
                ent.HasAlternateKey(p => p.Name);
            });
            modelBuilder.Entity<ServantSkill>(ent =>
            {
                ent.HasKey(p => p.Id);
            });
            modelBuilder.Entity<ServantTrait>(ent =>
            {
                ent.HasKey(p => p.Id);
                ent.HasAlternateKey(p => p.Trait);
            });
            modelBuilder.Entity<CEProfile>(ent =>
            {
                ent.HasKey(p => p.Id);
                ent.HasAlternateKey(p => p.Name);
            });
            modelBuilder.Entity<Event>(ent =>
            {
                ent.HasKey(p => p.Id);
                ent.HasAlternateKey(p => p.EventName);
            });
            modelBuilder.Entity<MysticCode>(ent =>
            {
                ent.HasKey(p => p.Id);
                ent.HasAlternateKey(p => p.Code);
            });
            modelBuilder.Entity<UserAccount>(ent =>
            {
                ent.Property(u => u.UserId).ForSqliteHasColumnType("INTEGER");
            });
            //base.OnModelCreating(modelBuilder);
        }
    }
}
