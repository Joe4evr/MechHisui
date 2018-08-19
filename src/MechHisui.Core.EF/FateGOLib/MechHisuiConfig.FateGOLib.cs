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
        public DbSet<CERange> CERanges { get; set; }
        public DbSet<CEAlias> CEAliases { get; set; }

        public DbSet<MysticCode> MysticCodes { get; set; }
        public DbSet<MysticAlias> MysticAliases { get; set; }

        public DbSet<FgoEvent> FgoEvents { get; set; }
        public DbSet<FgoEventGacha> EventGachas { get; set; }
        public DbSet<RateUpServant> RateUpServants { get; set; }

        public DbSet<NameOnlyServant> NameOnlyServants { get; set; }

        private static void ConfigureFgoModel(ModelBuilder modelBuilder)
        {
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

                servant.HasData(FgoSeedData.ServantProfiles());
            });

            modelBuilder.Entity<CEProfile>(ce =>
            {
                ce.HasMany(c => c.Aliases)
                    .WithOne(a => a.CE);

                ce.HasData(FgoSeedData.CEProfiles());
            });

            modelBuilder.Entity<CERange>(cer =>
            {
                cer.HasData(FgoSeedData.CERanges());
            });

            modelBuilder.Entity<ServantTrait>(trait =>
            {
                trait.HasData(FgoSeedData.ServantTraits());
            });

            modelBuilder.Entity<ServantProfileTrait>(pTrait =>
            {
                pTrait.HasOne(t => t.Servant)
                    .WithMany(p => p.Traits);

                pTrait.HasOne(t => t.Trait);
            });

            modelBuilder.Entity<ActiveSkill>(aSkill =>
            {
                aSkill.HasData(FgoSeedData.ActiveSkills());
            });

            modelBuilder.Entity<ServantActiveSkill>(skill =>
            {
                skill.HasOne(s => s.Servant)
                    .WithMany(p => p.ActiveSkills);

                skill.HasOne(s => s.Skill);
            });

            modelBuilder.Entity<PassiveSkill>(pSkill =>
            {
                pSkill.HasData(FgoSeedData.PassiveSkills());
            });

            modelBuilder.Entity<ServantPassiveSkill>(skill =>
            {
                skill.HasOne(s => s.Servant)
                    .WithMany(p => p.PassiveSkills);

                skill.HasOne(s => s.Skill);
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

                code.HasData(FgoSeedData.MysticCodes());
            });

            modelBuilder.Entity<MysticAlias>(alias =>
            {
                alias.HasData(FgoSeedData.MysticAliases());
            });

            modelBuilder.Entity<FgoEvent>(ev =>
            {
                ev.HasMany(e => e.EventGachas)
                    .WithOne(g => g.Event);
            });

            modelBuilder.Entity<FgoEventGacha>(gacha =>
            {
                gacha.HasMany(g => g.RateUpServants)
                    .WithOne(r => r.EventGacha);
            });

            modelBuilder.Entity<RateUpServant>(rateUp =>
            {
                rateUp.HasOne(r => r.EventGacha);
                rateUp.HasOne(r => r.Servant);
            });

            //modelBuilder.HasDbFunction(() => 0);
        }
    }
}
