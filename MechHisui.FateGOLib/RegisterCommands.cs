//using System;
//using System.Globalization;
//using System.Linq;
//using System.Threading;
//using Discord;
//using Discord.Commands;
//using JiiLib;
//using MechHisui.FateGOLib;

//namespace MechHisui.Commands
//{
//    public static class FgoExtensions
//    {
//        public static void RegisterAPCommand(this DiscordClient client, IConfiguration config)
//        {
//            Console.WriteLine("Registering 'AP'...");
//            client.GetService<CommandService>().CreateCommand("ap")
//                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
//                .Parameter("current amount", ParameterType.Optional)
//                .Parameter("time left", ParameterType.Optional)
//                .Description("Track your current AP.")
//                .Do(async cea =>
//                {
//                    FgoHelpers.UsersAP.RemoveAll(ap => ap.CurrentAP >= 140);
//                    if (cea.Args.All(s => s == String.Empty))
//                    {
//                        var userap = FgoHelpers.UsersAP.SingleOrDefault(u => u.UserID == cea.User.Id);
//                        if (userap != null)
//                        {
//                            await cea.Channel.SendMessage($"{cea.User.Name} currently has {userap.CurrentAP} AP.");
//                        }
//                        else
//                        {
//                            await cea.Channel.SendMessage($"Currently not tracking {cea.User.Name}'s AP.");
//                        }
//                        return;
//                    }

//                    int startAmount;
//                    TimeSpan startTime;
//                    if (Int32.TryParse(cea.Args[0], out startAmount) && TimeSpan.TryParseExact(cea.Args[1], @"m\:%s", CultureInfo.InvariantCulture, out startTime))
//                    {
//                        var tmp = FgoHelpers.UsersAP.SingleOrDefault(ap => ap.UserID == cea.User.Id);
//                        if (tmp != null)
//                        {
//                            FgoHelpers.UsersAP.Remove(tmp);
//                        }

//                        FgoHelpers.UsersAP.Add(new UserAP
//                        {
//                            UserID = cea.User.Id,
//                            StartAP = startAmount,
//                            StartTimeLeft = startTime
//                        });

//                        await cea.Channel.SendMessage($"Now tracking AP for `{cea.User.Name}`.");
//                    }
//                    else
//                    {
//                        await cea.Channel.SendMessage("One or both arguments could not be parsed correctly.");
//                    }
//                });
//        }

//        public static void RegisterDailyCommand(this DiscordClient client, IConfiguration config)
//        {
//            Console.WriteLine("Registering 'Daily'...");
//            client.GetService<CommandService>().CreateCommand("daily")
//                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
//                .Parameter("day", ParameterType.Optional)
//                .Description("Relay the information of daily quests for the specified day. Default to current day.")
//                .Do(async cea =>
//                {
//                    DayOfWeek day;
//                    ServantClass serv;
//                    DailyInfo info;

//                    var todayInJapan = new DateTimeWithZone(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
//                    var arg = cea.Args[0];

//                    if (String.IsNullOrWhiteSpace(arg) || arg == "today")
//                    {
//                        day = todayInJapan.LocalTime.DayOfWeek;
//                    }
//                    else if (arg == "change")
//                    {
//                        var eta = todayInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 0, minutes: 0, seconds: 0));
//                        string h = eta.Hours == 1 ? "hour" : "hours";
//                        string m = eta.Minutes == 1 ? "minute" : "minutes";
//                        await cea.Channel.SendMessage($"Daily quests changing **ETA: {eta.Hours} {h} and {eta.Minutes} {m}.**");
//                        return;
//                    }
//                    else if (arg == "tomorrow")
//                    {
//                        day = todayInJapan.LocalTime.AddDays(1).DayOfWeek;
//                    }
//                    else if (Enum.TryParse(arg, ignoreCase: true, result: out serv))
//                    {
//                        day = DailyInfo.DailyQuests.SingleOrDefault(d => d.Value.Materials == serv).Key;
//                    }
//                    else if (!Enum.TryParse(arg, ignoreCase: true, result: out day))
//                    {
//                        await cea.Channel.SendMessage("Could not convert argument to a day of the week or Servant class. Please try again.");
//                        return;
//                    }

//                    if (DailyInfo.DailyQuests.TryGetValue(day, out info))
//                    {
//                        bool isToday = (day == todayInJapan.LocalTime.DayOfWeek);
//                        bool isTomorrow = (day == todayInJapan.LocalTime.AddDays(1).DayOfWeek);
//                        string whatDay = isToday ? "Today" : (isTomorrow ? "Tomorrow" : day.ToString());
//                        if (day != DayOfWeek.Sunday)
//                        {
//                            await cea.Channel.SendMessage($"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tExperience: **{info.Exp1.ToString()}**, **{info.Exp2.ToString()}**, and **{ServantClass.Berserker.ToString()}**");
//                        }
//                        else
//                        {
//                            await cea.Channel.SendMessage($"{whatDay}'s quests:\n\tAscension Materials: **{info.Materials.ToString()}**\n\tExperience: **Any Class**\n\tAnd also **QP**");
//                        }
//                    }
//                });
//        }

//        public static void RegisterLoginBonusCommand(this DiscordClient client, IConfiguration config)
//        {
//            Console.WriteLine("Registering 'Login bonus'...");
//            client.GetService<CommandService>().CreateCommand("login")
//                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
//                .Description("Relay the information of the arrival of the next login bonus.")
//                .Do(async cea =>
//                {
//                    var rightNowInJapan = new DateTimeWithZone(DateTime.UtcNow, FgoHelpers.JpnTimeZone);
//                    TimeSpan eta = rightNowInJapan.TimeUntilNextLocalTimeAt(new TimeSpan(hours: 4, minutes: 0, seconds: 0));
//                    string h = eta.Hours == 1 ? "hour" : "hours";
//                    string m = eta.Minutes == 1 ? "minute" : "minutes";
//                    await cea.Channel.SendMessage($"Next login bonus drop **ETA {eta.Hours} {h} and {eta.Minutes} {m}.**");
//                });

//            LoginBonusTimer = new Timer(async cb =>
//            {
//                Console.WriteLine("Announcing login bonuses.");
//                await ((DiscordClient)cb).GetChannel(UInt64.Parse(config["FGO_general"]))
//                    .SendMessage("Login bonuses have been distributed.");
//            },
//            client,
//            new DateTimeWithZone(DateTime.UtcNow, FgoHelpers.JpnTimeZone)
//                .TimeUntilNextLocalTimeAt(new TimeSpan(hours: 3, minutes: 59, seconds: 58)),
//            TimeSpan.FromDays(1));
//        }

//        public static void RegisterQuartzCommand(this DiscordClient client, IConfiguration config)
//        {
//            Console.WriteLine("Registering 'Quartz'...");
//            client.GetService<CommandService>().CreateCommand("quartz")
//                .AddCheck((c, u, ch) => ch.Server.Id == UInt64.Parse(config["FGO_server"]))
//                .Description("Relay the prices of different amounts of Saint Quartz.")
//                .Do(async cea =>
//                    await cea.Channel.SendMessage(
//@"Prices for Quartz:
//```
//  1Q:  120 JPY
//  5Q:  480 JPY
// 16Q: 1400 JPY
// 36Q: 2900 JPY
// 65Q: 4800 JPY
//140Q: 9800 JPY
//```"));
//        }

//        public static void RegisterZoukenCommand(this DiscordClient client, IConfiguration config)
//        {
//            client.GetService<CommandService>().CreateCommand("zouken")
//                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(config["FGO_general"]) || ch.Id == UInt64.Parse(config["FGO_playground"]))
//                .Hide()
//                .Do(async cea =>
//                {
//                    await cea.Channel.SendMessage("Friendly reminder that the Zouken CE doesn't trigger on suicides, so don't even think about pairing it with A-Trash.");
//                });
//        }

//        private static Timer LoginBonusTimer;

//        private static decimal SimulateDmg(ServantProfile srv, string enemyClass, int servantAtk, Card atkCard, int atkIndex)
//        {
//            var npDamageMultiplier = 1.0m;
//            var firstCardBonus = atkCard == Card.Buster ? 0.5m : 0m;
//            var cardDamageValue = calcCardDmg(atkCard, (--atkIndex));
//            var cardMod = 0m;
//            var classAtkBonus = getClassBonus(srv.Class);
//            var triangleModifier = getTriMod(srv.Class, enemyClass);
//            var attributeModifier = 1.0m;
//            var rng = new Random();

//            double rand;
//            do rand = rng.NextDouble();
//            while (rand < 0.9d || rand > 1.1d);

//            var randomModifier = Convert.ToDecimal(rand);
//            var atkMod = 0m;
//            var defMod = 0m;
//            var criticalModifier = 1.0m;
//            var extraCardModifier = atkCard == Card.Extra ? 2.0m : 1.0m;
//            var powerMod = 0m;
//            var selfDamageMod = 0m;
//            var critDamageMod = 0m;
//            var isCrit = 0;
//            var npDamageMod = 1.0m;
//            var isNP = 0;
//            var superEffectiveModifier = 1.0m;
//            var isSuperEffective = 0;
//            var dmgPlusAdd = 0;
//            var selfDmgCutAdd = 0;
//            var busterChainMod = 0;

//            return (servantAtk * npDamageMultiplier *
//                (firstCardBonus +
//                    (cardDamageValue *
//                        (1 + cardMod))) *
//                classAtkBonus * triangleModifier * attributeModifier * randomModifier * 0.23m *
//                (1 + atkMod - defMod) *
//                criticalModifier * extraCardModifier *
//                (1 + powerMod + selfDamageMod +
//                    (critDamageMod * isCrit) +
//                    (npDamageMod * isNP)) *
//                (1 + ((superEffectiveModifier - 1) *
//                    isSuperEffective))) +
//            dmgPlusAdd + selfDmgCutAdd + (servantAtk * busterChainMod);
//        }

//        private static decimal getClassBonus(string srvClass)
//        {
//            switch (srvClass)
//            {
//                case "Archer":
//                    return 0.95m;
//                case "Caster":
//                case "Assassin":
//                    return 0.9m;
//                case "Saber":
//                case "Rider":
//                case "Shielder":
//                case "Alter-Ego":
//                case "Beast":
//                default:
//                    return 1.0m;
//                case "Lancer":
//                    return 1.05m;
//                case "Berserker":
//                case "Ruler":
//                case "Avenger":
//                    return 1.1m;
//            }
//        }

//        private static decimal getTriMod(string attacker, string defender)
//        {
//            switch (attacker)
//            {
//                case "Saber":
//                    switch (defender)
//                    {
//                        case "Archer":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Saber":
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Shielder":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Lancer":
//                        case "Berserker":
//                        case "Alter-Ego":
//                        case "Avenger":
//                            return 2.0m;
//                    }
//                case "Archer":
//                    switch (defender)
//                    {
//                        case "Lancer":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Archer":
//                        case "Caster":
//                        case "Assassin":
//                        case "Rider":
//                        case "Shielder":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Saber":
//                        case "Berserker":
//                        case "Alter-Ego":
//                        case "Avenger":
//                            return 2.0m;
//                    }
//                case "Lancer":
//                    switch (defender)
//                    {
//                        case "Saber":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Lancer":
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Shielder":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Archer":
//                        case "Berserker":
//                        case "Alter-Ego":
//                        case "Avenger":
//                            return 2.0m;
//                    }
//                case "Rider":
//                    switch (defender)
//                    {
//                        case "Assassin":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Rider":
//                        case "Shielder":
//                        case "Alter-Ego":
//                        default:
//                            return 1.0m;
//                        case "Caster":
//                        case "Berserker":
//                        case "Avenger":
//                        case "Beast":
//                            return 2.0m;
//                    }
//                case "Caster":
//                    switch (defender)
//                    {
//                        case "Rider":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Caster":
//                        case "Shielder":
//                        default:
//                            return 1.0m;
//                        case "Assassin":
//                        case "Berserker":
//                        case "Alter-Ego":
//                        case "Avenger":
//                        case "Beast":
//                            return 2.0m;
//                    }
//                case "Assassin":
//                    switch (defender)
//                    {
//                        case "Caster":
//                        case "Ruler":
//                            return 0.5m;
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Assassin":
//                        case "Shielder":
//                        default:
//                            return 1.0m;
//                        case "Rider":
//                        case "Berserker":
//                        case "Alter-Ego":
//                        case "Avenger":
//                        case "Beast":
//                            return 2.0m;
//                    }
//                case "Berserker":
//                    return defender == "Shielder" ? 1.0m : 1.5m;
//                case "Shielder":
//                    return 1.0m;
//                case "Ruler":
//                    switch (defender)
//                    {
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Shielder":
//                        case "Ruler":
//                        case "Alter-Ego":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Berserker":
//                        case "Avenger":
//                            return 2.0m;
//                    }
//                case "Alter-Ego":
//                    switch (defender)
//                    {
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Shielder":
//                        case "Ruler":
//                        case "Alter-Ego":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Berserker":
//                        case "Avenger":
//                            return 2.0m;
//                    }
//                case "Avenger":
//                    switch (defender)
//                    {
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Shielder":
//                        case "Alter-Ego":
//                        case "Avenger":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Berserker":
//                        case "Ruler":
//                            return 2.0m;
//                    }
//                case "Beast":
//                    switch (defender)
//                    {
//                        case "Avenger":
//                            return 0.5m;
//                        case "Rider":
//                        case "Caster":
//                        case "Assassin":
//                        case "Shielder":
//                        case "Ruler":
//                        case "Alter-Ego":
//                        case "Beast":
//                        default:
//                            return 1.0m;
//                        case "Saber":
//                        case "Archer":
//                        case "Lancer":
//                        case "Berserker":
//                            return 2.0m;
//                    }
//                default:
//                    return 1.0m;
//            }
//        }

//        private static decimal calcCardDmg(Card atk, int index)
//        {
//            switch (atk)
//            {
//                case Card.Arts:
//                    return 1.0m + (0.2m * index);
//                case Card.Buster:
//                    return 1.5m + (0.3m * index);
//                case Card.Quick:
//                    return 0.8m + (0.16m * index);
//                default:
//                    return 1.0m;
//            }
//        }
//    }
//}
