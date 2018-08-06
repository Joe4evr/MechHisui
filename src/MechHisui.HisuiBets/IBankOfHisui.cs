using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace MechHisui.HisuiBets
{
    public interface IBankOfHisui
    {
        char CurrencySymbol { get; }

        IBankAccount GetAccount(IUser user);
        Task<IEnumerable<IBankAccount>> GetAllUsersAsync();

        IBetGame CreateGame(ITextChannel channel, GameType gameType);
        Task<IEnumerable<IBetGame>> GetUncashedGamesAsync();
        Task<IBetGame> GetLastGameInChannelAsync(ITextChannel channel);
        Task<IBetGame> GetGameInChannelByIdAsync(ITextChannel channel, int gameId);

        Task<RecordingResult> RecordOrUpdateBetAsync(IBetGame game, IBet bet);
        Task<IBet> RetrieveBetAsync(IUser user, IBetGame game);

        Task<IBankAccount> AddUserAsync(IUser user);
        Task AddUsersAsync(IEnumerable<IUser> users);

        Task<BetResult> CashOutAsync(BetCollection bets, string winner);
        Task<DonationResult> DonateAsync(DonationRequest request);

        Task<WithdrawalResult> WithdrawAsync(WithdrawalRequest request);
        Task CollectBetsAsync(int gameId);
        Task InterestAsync();

        Task AddToVaultAsync(int amount);
        Task<int> GetVaultWorthAsync();
        Task<int> RetrieveFromVaultAsync(int amount);
    }
}
