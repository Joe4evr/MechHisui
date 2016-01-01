using System;
using System.Collections.Generic;

namespace MechHisui.FateGOLib
{
    public class ServantProfile
    {
        public int Id { get; set; }
        public string Class { get; set; }
        public string Rarity { get; set; }
        public string Name { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public int Starweight { get; set; }
        public string GrowthCurve { get; set; }
        public string CardPool { get; set; }
        public string NoblePhantasm { get; set; }
        public string NoblePhantasmEffect { get; set; }
        public string Traits { get; set; }
        public string Attribute { get; set; }
        public string Skill1 { get; set; }
        public string Effect1 { get; set; }
        public string Skill2 { get; set; }
        public string Effect2 { get; set; }
        public string Skill3 { get; set; }
        public string Effect3 { get; set; }
        public string PassiveSkill1 { get; set; }
        public string PEffect1 { get; set; }
        public string PassiveSkill2 { get; set; }
        public string PEffect2 { get; set; }
        public string PassiveSkill3 { get; set; }
        public string PEffect3 { get; set; }
        public string PassiveSkill4 { get; set; }
        public string PEffect4 { get; set; }
        public string Image { get; set; }
    }

    public class ServantAlias
    {
        public IList<string> Alias { get; set; }
        public string Servant { get; set; }
    }

    public class CEProfile
    {
        public int Id { get; set; }
        public string Rarity { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int Atk { get; set; }
        public int HP { get; set; }
        public string Effect { get; set; }
        public int AtkMax { get; set; }
        public int HPMax { get; set; }
        public string EffectMax { get; set; }
        public string Image { get; set; }
    }

    public class CEAlias
    {
        public IList<string> Alias { get; set; }
        public string CE { get; set; }
    }

    public class Event
    {
        public string EventName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string EventGacha { get; set; }
    }

    public class MysticCode
    {
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
        public IList<string> Alias { get; set; }
        public string Code { get; set; }
    }
}
