using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.FateGOLib.Modules
{
    public class SimulateModule : IModule
    {
        private readonly StatService _statService;
        private readonly IConfiguration _config;

        public SimulateModule(StatService statService, IConfiguration config)
        {
            _statService = statService;
            _config = config;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Simulate'...");
            manager.Client.GetService<CommandService>().CreateCommand("simdmg")
                .AddCheck((c, u, ch) => ch.Id == UInt64.Parse(_config["FGO_playground"]))
                .Description("Roughly approximate an attacks damage (not accounting for NP, Crit, buffs/debuffs).")
                .Parameter("servant", ParameterType.Required)
                .Parameter("enemyClass", ParameterType.Required)
                .Parameter("atk", ParameterType.Required)
                .Parameter("atkCard", ParameterType.Required)
                .Parameter("atkIndex", ParameterType.Required)
                .Do(async cea =>
                {
                    int atk;
                    if (!Int32.TryParse(cea.GetArg("atk"), out atk))
                    {
                        await cea.Channel.SendMessage("Could not parse `atk` parameter as number.");
                        return;
                    }

                    Card atkCard;
                    if (!Enum.TryParse<Card>(cea.GetArg("atkCard"), true, out atkCard))
                    {
                        await cea.Channel.SendMessage("Could not parse `atkCard` parameter as a valid attack type.");
                        return;
                    }

                    int index;
                    if (atkCard != Card.Extra)
                    {
                        if (!Int32.TryParse(cea.GetArg("atkIndex"), out index))
                        {
                            await cea.Channel.SendMessage("Could not parse `atkIndex` parameter as a number.");
                            return;
                        }
                        else if (index < 1 || index > 3)
                        {
                            await cea.Channel.SendMessage("Parameter `atkIndex` not in valid range.");
                            return;
                        }
                    }
                    else
                    {
                        index = 4;
                    }


                    ServantProfile profile;
                    int id;
                    if (Int32.TryParse(cea.GetArg("servant"), out id))
                    {
                        profile = FgoHelpers.ServantProfiles.SingleOrDefault(p => p.Id == id);
                    }
                    else
                    {
                        profile = _statService.LookupStats(cea.GetArg("servant"));
                    }

                    if (profile == null)
                    {
                        await cea.Channel.SendMessage("Could not find specified Servant.");
                        return;
                    }
                    else
                    {
                        await cea.Channel.SendMessage($"**Approximate damage dealt:** {SimulateDmg(profile, cea.GetArg("enemyClass"), atk, atkCard, index):N3}");
                        return;
                    }
                });
        }

        private static decimal SimulateDmg(ServantProfile srv, string enemyClass, int servantAtk, Card atkCard, int atkIndex)
        {
            var npDamageMultiplier = 1.0m;
            var firstCardBonus = atkCard == Card.Buster ? 0.5m : 0m;
            var cardDamageValue = calcCardDmg(atkCard, (--atkIndex));
            var cardMod = 0m;
            var classAtkBonus = getClassBonus(srv.Class);
            var triangleModifier = getTriMod(srv.Class, enemyClass);
            var attributeModifier = 1.0m;
            var rng = new Random();

            double rand;
            do rand = rng.NextDouble();
            while (rand < 0.9d || rand > 1.1d);

            var randomModifier = Convert.ToDecimal(rand);
            var atkMod = 0m;
            var defMod = 0m;
            var criticalModifier = 1.0m;
            var extraCardModifier = atkCard == Card.Extra ? 2.0m : 1.0m;
            var powerMod = 0m;
            var selfDamageMod = 0m;
            var critDamageMod = 0m;
            var isCrit = 0;
            var npDamageMod = 1.0m;
            var isNP = 0;
            var superEffectiveModifier = 1.0m;
            var isSuperEffective = 0;
            var dmgPlusAdd = 0;
            var selfDmgCutAdd = 0;
            var busterChainMod = 0;

            return (servantAtk * npDamageMultiplier *
                (firstCardBonus +
                    (cardDamageValue *
                        (1 + cardMod))) *
                classAtkBonus * triangleModifier * attributeModifier * randomModifier * 0.23m *
                (1 + atkMod - defMod) *
                criticalModifier * extraCardModifier *
                (1 + powerMod + selfDamageMod +
                    (critDamageMod * isCrit) +
                    (npDamageMod * isNP)) *
                (1 + ((superEffectiveModifier - 1) *
                    isSuperEffective))) +
            dmgPlusAdd + selfDmgCutAdd + (servantAtk * busterChainMod);
        }

        private static decimal getClassBonus(string srvClass)
        {
            switch (srvClass)
            {
                case "Archer":
                    return 0.95m;
                case "Caster":
                case "Assassin":
                    return 0.9m;
                case "Saber":
                case "Rider":
                case "Shielder":
                case "Alter-Ego":
                case "Beast":
                default:
                    return 1.0m;
                case "Lancer":
                    return 1.05m;
                case "Berserker":
                case "Ruler":
                case "Avenger":
                    return 1.1m;
            }
        }

        private static decimal getTriMod(string attacker, string defender)
        {
            switch (attacker)
            {
                case "Saber":
                    switch (defender)
                    {
                        case "Archer":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Lancer":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Archer":
                    switch (defender)
                    {
                        case "Lancer":
                        case "Ruler":
                            return 0.5m;
                        case "Archer":
                        case "Caster":
                        case "Assassin":
                        case "Rider":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Saber":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Lancer":
                    switch (defender)
                    {
                        case "Saber":
                        case "Ruler":
                            return 0.5m;
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Archer":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Rider":
                    switch (defender)
                    {
                        case "Assassin":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Shielder":
                        case "Alter-Ego":
                        default:
                            return 1.0m;
                        case "Caster":
                        case "Berserker":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Caster":
                    switch (defender)
                    {
                        case "Rider":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Caster":
                        case "Shielder":
                        default:
                            return 1.0m;
                        case "Assassin":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Assassin":
                    switch (defender)
                    {
                        case "Caster":
                        case "Ruler":
                            return 0.5m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Assassin":
                        case "Shielder":
                        default:
                            return 1.0m;
                        case "Rider":
                        case "Berserker":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                            return 2.0m;
                    }
                case "Berserker":
                    return defender == "Shielder" ? 1.0m : 1.5m;
                case "Shielder":
                    return 1.0m;
                case "Ruler":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Berserker":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Alter-Ego":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Berserker":
                        case "Avenger":
                            return 2.0m;
                    }
                case "Avenger":
                    switch (defender)
                    {
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Alter-Ego":
                        case "Avenger":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Berserker":
                        case "Ruler":
                            return 2.0m;
                    }
                case "Beast":
                    switch (defender)
                    {
                        case "Avenger":
                            return 0.5m;
                        case "Rider":
                        case "Caster":
                        case "Assassin":
                        case "Shielder":
                        case "Ruler":
                        case "Alter-Ego":
                        case "Beast":
                        default:
                            return 1.0m;
                        case "Saber":
                        case "Archer":
                        case "Lancer":
                        case "Berserker":
                            return 2.0m;
                    }
                default:
                    return 1.0m;
            }
        }

        private static decimal calcCardDmg(Card atk, int index)
        {
            switch (atk)
            {
                case Card.Arts:
                    return 1.0m + (0.2m * index);
                case Card.Buster:
                    return 1.5m + (0.3m * index);
                case Card.Quick:
                    return 0.8m + (0.16m * index);
                default:
                    return 1.0m;
            }
        }
    }
}
