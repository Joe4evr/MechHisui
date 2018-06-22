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
        Task<IEnumerable<IBankAccount>> GetAllUsers();

        IBetGame CreateGame(ITextChannel channel, GameType gameType);
        Task<IEnumerable<IBetGame>> GetUncashedGames();
        Task<IBetGame> GetLastGameInChannel(ITextChannel channel);
        Task<IBetGame> GetGameInChannelById(ITextChannel channel, int gameId);

        Task<RecordingResult> RecordOrUpdateBet(IBetGame game, IBet bet);
        Task<IBet> RetrieveBet(IUser user, IBetGame game);

        Task<IBankAccount> AddUser(IUser user);
        Task AddUsers(IEnumerable<IUser> users);

        Task<BetResult> CashOut(BetCollection bets, string winner);
        Task<DonationResult> Donate(DonationRequest request);

        Task<WithdrawalResult> Withdraw(WithdrawalRequest request);
        Task CollectBets(IBetGame game);
        Task Interest();

        Task AddToVault(int amount);
        Task<int> RetrieveFromVault(uint amount);
    }
}
