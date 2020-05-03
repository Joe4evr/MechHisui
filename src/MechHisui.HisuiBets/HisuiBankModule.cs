using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Preconditions;
using Discord.Addons.SimplePermissions;
using System.Linq;
using SharedExtensions;

namespace MechHisui.HisuiBets
{
    [Name("Bank of Hisui"), RequireContext(ContextType.Guild)]
    [Permission(MinimumPermission.Everyone)]
    public class HisuiBankModule : ModuleBase<SocketCommandContext>
    {
        private readonly HisuiBankService _service;

        private IBankAccount? _account;

        public HisuiBankModule(HisuiBankService service)
        {
            _service = service;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            _account = _service.Bank.GetAccount(Context.User);
        }

        [Command("createacc"), Permission(MinimumPermission.ModRole)]
        public async Task CreateAccountCmd(IUser user)
        {
            await _service.Bank.AddUserAsync(user).ConfigureAwait(false);
            await ReplyAsync($"Created bank account for **{user.Username}**").ConfigureAwait(false);
        }

        [Command("bucks"), Alias("mybucks")]
        public Task Bucks()
            => ReplyAsync($"**{Context.User.Username}** currently has {_service.Bank.CurrencySymbol}{_account!.Balance}.");

        [Command("donate"), Ratelimit(5, 10, Measure.Minutes)]
        public async Task Donate(int amount, IUser recipient)
        {
            if (amount <= 0)
            {
                await ReplyAsync("Cannot make a donation of 0 or less.").ConfigureAwait(false);
            }
            if (recipient.IsBot || _service.Blacklist.Contains(recipient.Id))
            {
                await ReplyAsync("Not allowed to donate to that account.").ConfigureAwait(false);
            }

            var donationResult = await _service.Bank.DonateAsync(new DonationRequest((uint)amount, _account!, recipient)).ConfigureAwait(false);
            switch (donationResult)
            {
                case DonationResult.DonationSuccess:
                    await ReplyAsync($"**{Context.User.Username}** donated {_service.Bank.CurrencySymbol}{amount} to **{recipient.Username}**.").ConfigureAwait(false);
                    return;

                case DonationResult.DonorNotEnoughMoney:
                    await ReplyAsync($"**{Context.User.Username}** currently does not have enough HisuiBucks to make that donation.").ConfigureAwait(false);
                    return;

                case DonationResult.DonorNotFound:
                case DonationResult.RecipientNotFound:
                case DonationResult.MiscError:
                default:
                    await ReplyAsync("Failed to transfer donation.").ConfigureAwait(false);
                    return;
            }
        }

        [Command("top"), Permission(MinimumPermission.Special)]
        public async Task Tops()
        {
            var tops = (await _service.Bank.GetAllUsersAsync().ConfigureAwait(false))
                //.Where(a => a.Balance > 2500)
                .OrderByDescending(a => a.Balance)
                .Take(10)
                .Select(a => new
                {
                    Name = Context.Client.GetUser(a.UserId).Username,
                    a.Balance
                })
                .ToList();

            var sb = new StringBuilder("```\n")
                .AppendSequence(tops, (s, a) => s.AppendLine($"{a.Name,-20}: {_service.Bank.CurrencySymbol}{a.Balance,7}"))
                .Append("```");

            await ReplyAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("vault"), Permission(MinimumPermission.BotOwner), Hidden]
        public async Task Vault()
        {
            var contents = await _service.Bank.GetVaultWorthAsync().ConfigureAwait(false);
            await ReplyAsync($"Vault currently contains {_service.Bank.CurrencySymbol}{contents}").ConfigureAwait(false);
        }
    }
}
