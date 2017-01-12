using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class ServantProfile
    {
        public int Id { get; set; }
        public string Class { get; set; }
        public int Rarity { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public int Starweight { get; set; }
        public string Gender { get; set; }
        public string GrowthCurve { get; set; }
        public string CardPool { get; set; }
        public int B { get; set; }
        public int A { get; set; }
        public int Q { get; set; }
        public int EX { get; set; }
        public string NoblePhantasm { get; set; }
        public string NoblePhantasmEffect { get; set; }
        public string NoblePhantasmRankUpEffect { get; set; }
        public ICollection<ServantTrait> Traits { get; set; }
        public string Attribute { get; set; }
        public ICollection<ServantSkill> ActiveSkills { get; set; }
        public ICollection<ServantSkill> PassiveSkills { get; set; }
        public string Additional { get; set; }
        public string Image { get; set; }
        public bool Obtainable { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    public class ServantTrait
    {
        public int Id { get; set; }
        public string Trait { get; set; }
        public bool IsAutoComputed { get; set; }
    }

    public class ServantSkill
    {
        public int Id { get; set; }
        public string SkillName { get; set; }
        public string Rank { get; set; }
        public string Effect { get; set; }
        public string RankUpEffect { get; set; }
    }

    public class ServantAlias
    {
        public string[] Alias { get; set; }
        public string Servant { get; set; }
    }

    public class CEProfile
    {
        public int Id { get; set; }
        public int Rarity { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public string Effect { get; set; }
        public string EventEffect { get; set; }
        public int AtkMax { get; set; }
        public int HPMax { get; set; }
        public string EffectMax { get; set; }
        public string EventEffectMax { get; set; }
        public string Image { get; set; }
        public bool Obtainable { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    public class CEAlias
    {
        public string[] Alias { get; set; }
        public string CE { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string EventGacha { get; set; }
        public string InfoLink { get; set; }
    }

    public class MysticCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Skill1 { get; set; }
        public string Skill1Effect { get; set; }
        public string Skill2 { get; set; }
        public string Skill2Effect { get; set; }
        public string Skill3 { get; set; }
        public string Skill3Effect { get; set; }
        public string Image { get; set; }
    }

    public class MysticAlias
    {
        public string[] Alias { get; set; }
        public string Code { get; set; }
    }

    public class NodeDrop
    {
        public string Map { get; set; }
        public string NodeJP { get; set; }
        public string NodeEN { get; set; }
        public string ItemDrops { get; set; }
    }

    public class NameOnlyServant
    {
        public string Class { get; set; }
        public string Name { get; set; }
    }

    public class UserAP
    {
        public int StartAP { get; set; }
        public ulong UserID { get; set; }
        public TimeSpan StartTimeLeft { get; set; }
        public DateTime StartTime { get; } = DateTime.UtcNow;
        //public int CurrentAP => StartAP + (int)Math.Floor(
        //    (DateTime.UtcNow - (StartTime - StartTimeLeft))
        //    .TotalMinutes / FgoHelpers.PerAP.TotalMinutes);
    }

    public class FriendData
    {
        public int Id { get; set; }
        public ulong User { get; set; }
        public string FriendCode { get; set; }
        public string Class { get; set; }
        public string Servant { get; set; }
    }

    public enum Card
    {
        Arts,
        Buster,
        Quick,
        Extra
    }
}
