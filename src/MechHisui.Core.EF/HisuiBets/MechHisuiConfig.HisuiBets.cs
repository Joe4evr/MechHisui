using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MechHisui.Core
{
    public partial class MechHisuiConfig
    {
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BetGame> BetGames { get; set; }
        public DbSet<Bet> RecordedBets { get; set; }

        private static void ConfigureBetsModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BankAccount>(account =>
            {
                account.Property(ac => ac.Balance)
                    .HasDefaultValue(1500);

                //account.Property(ac => ac.UserId)
                //    .IsRequired(true)
                //    .HasConversion(ul => unchecked((long)ul), l => unchecked((ulong)l));

                //account.HasIndex(ac => ac.UserId)
                //    .IsUnique(true);

                account.Property<long>("UserSnowflake")
                    .HasField(nameof(BankAccount._uid))
                    .IsRequired(true);

                account.HasIndex("UserSnowflake")
                    .IsUnique(true);
            });

            modelBuilder.Entity<BetGame>(game =>
            {
                game.HasOne(g => g.Channel);

                //game.Property(g => g.Channel)
                //    .IsRequired(true);

                game.HasMany(g => g.Bets)
                    .WithOne(b => b.BetGame);

                game.Property(g => g.IsCashedOut)
                    .IsRequired(true)
                    .HasDefaultValue(false);

                game.Property(g => g.IsCollected)
                    .IsRequired(true)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Bet>(bet =>
            {
                bet.HasOne(b => b.User);
            });
        }
    }
}
