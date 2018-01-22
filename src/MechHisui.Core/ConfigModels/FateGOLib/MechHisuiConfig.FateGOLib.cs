using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig
    {
        //FGO
        public DbSet<ServantProfile> Servants { get; set; }
        public DbSet<ActiveSkill> FgoActiveSkills { get; set; }
        public DbSet<PassiveSkill> FgoPassiveSkills { get; set; }
        public DbSet<ServantTrait> Traits { get; set; }
        public DbSet<ServantAlias> ServantAliases { get; set; }

        public DbSet<ServantActiveSkill> ProfileActiveSkills { get; set; }
        public DbSet<ServantPassiveSkill> ProfilePassiveSkills { get; set; }
        public DbSet<ServantProfileTrait> ProfileTraits { get; set; }

        public DbSet<CEProfile> CEs { get; set; }
        public DbSet<CEAlias> CEAliases { get; set; }

        public DbSet<MysticCode> MysticCodes { get; set; }
        public DbSet<MysticAlias> MysticAliases { get; set; }

        public DbSet<FgoEvent> FgoEvents { get; set; }

        public DbSet<NameOnlyServant> NameOnlyServants { get; set; }

        public DbSet<FgoFriendData> FgoFriendData { get; set; }

        private static void ConfigureFgoModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HisuiUser>()
                .HasOne(u => u.FriendData)
                .WithOne(f => f.User)
                .HasForeignKey<FgoFriendData>(f => f.UserFK);


            modelBuilder.Entity<ServantProfile>()
                .HasOne(p => p.Bond10);

            modelBuilder.Entity<ServantProfile>()
                .HasMany(p => p.Traits)
                .WithOne(t => t.Servant);

            modelBuilder.Entity<ServantProfileTrait>()
                .HasOne(t => t.Servant)
                .WithMany(p => p.Traits);

            modelBuilder.Entity<ServantProfileTrait>()
                .HasOne(t => t.Trait);

            modelBuilder.Entity<ServantTrait>()
                .HasMany(t => t.Servants)
                .WithOne(t => t.Trait);

            modelBuilder.Entity<ServantProfile>()
                .HasMany(p => p.ActiveSkills)
                .WithOne(s => s.Servant);

            modelBuilder.Entity<ServantActiveSkill>()
                .HasOne(s => s.Servant)
                .WithMany(p => p.ActiveSkills);

            modelBuilder.Entity<ServantActiveSkill>()
                .HasOne(s => s.Skill)
                .WithMany(a => a.Servants);

            modelBuilder.Entity<ActiveSkill>()
                .HasMany(a => a.Servants)
                .WithOne(s => s.Skill);

            modelBuilder.Entity<ServantProfile>()
                .HasMany(p => p.PassiveSkills)
                .WithOne(s => s.Servant);

            modelBuilder.Entity<ServantPassiveSkill>()
                .HasOne(s => s.Servant)
                .WithMany(p => p.PassiveSkills);

            modelBuilder.Entity<ServantPassiveSkill>()
                .HasOne(s => s.Skill)
                .WithMany(p => p.Servants);

            modelBuilder.Entity<ServantProfile>()
                .HasMany(p => p.Aliases)
                .WithOne(a => a.Servant);

            modelBuilder.Entity<ServantAlias>()
                .HasOne(a => a.Servant)
                .WithMany(p => p.Aliases);


            modelBuilder.Entity<CEProfile>()
                .HasMany(c => c.Aliases)
                .WithOne(a => a.CE);


            modelBuilder.Entity<MysticCode>()
                .HasMany(m => m.Aliases)
                .WithOne(a => a.Code);
        }
    }
}
