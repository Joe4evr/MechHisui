﻿using System;
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
        public DbSet<CERange> CERanges { get; set; }
        public DbSet<CEAlias> CEAliases { get; set; }

        public DbSet<MysticCode> MysticCodes { get; set; }
        public DbSet<MysticAlias> MysticAliases { get; set; }

        public DbSet<FgoEvent> FgoEvents { get; set; }

        public DbSet<NameOnlyServant> NameOnlyServants { get; set; }

        public DbSet<FgoFriendData> FgoFriendData { get; set; }

        private static void ConfigureFgoModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HisuiUser>(user =>
            {
                user.HasOne(u => u.FriendData);
            });

            modelBuilder.Entity<ServantProfile>(servant =>
            {
                servant.HasOne(p => p.Bond10);

                servant.HasMany(p => p.Traits)
                    .WithOne(t => t.Servant);

                servant.HasMany(p => p.ActiveSkills)
                    .WithOne(s => s.Servant);

                servant.HasMany(p => p.PassiveSkills)
                    .WithOne(s => s.Servant);

                servant.HasMany(p => p.Aliases)
                    .WithOne(a => a.Servant);
            });

            modelBuilder.Entity<CEProfile>(ce =>
            {
                ce.HasMany(c => c.Aliases)
                    .WithOne(a => a.CE);
            });

            modelBuilder.Entity<ServantTrait>(trait =>
            {
                trait.HasMany(t => t.Servants)
                    .WithOne(t => t.Trait);
            });

            modelBuilder.Entity<ServantProfileTrait>(trait =>
            {
                trait.HasOne(t => t.Servant)
                    .WithMany(p => p.Traits);

                trait.HasOne(t => t.Trait);
            });

            modelBuilder.Entity<ActiveSkill>(aSkill =>
            {
                aSkill.HasMany(a => a.Servants)
                    .WithOne(s => s.Skill);
            });

            modelBuilder.Entity<ServantActiveSkill>(skill =>
            {
                skill.HasOne(s => s.Servant)
                    .WithMany(p => p.ActiveSkills);

                skill.HasOne(s => s.Skill)
                    .WithMany(a => a.Servants);
            });

            modelBuilder.Entity<PassiveSkill>(pSkill =>
            {
                pSkill.HasMany(p => p.Servants)
                    .WithOne(s => s.Skill);
            });

            modelBuilder.Entity<ServantPassiveSkill>(skill =>
            {
                skill.HasOne(s => s.Servant)
                    .WithMany(p => p.PassiveSkills);

                skill.HasOne(s => s.Skill)
                    .WithMany(p => p.Servants);
            });

            modelBuilder.Entity<ServantAlias>(alias =>
            {
                alias.HasOne(a => a.Servant)
                    .WithMany(p => p.Aliases);
            });

            modelBuilder.Entity<MysticCode>(code =>
            {
                code.HasMany(m => m.Aliases)
                    .WithOne(a => a.Code);
            });
        }
    }
}
