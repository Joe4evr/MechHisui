using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechHisui.FateGOLib
{
    public class FgoProfileConverter : CustomCreationConverter<List<ServantProfile>>
    {
        public override List<ServantProfile> Create(Type objectType) => new List<ServantProfile>();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var mappedObj = new List<ServantProfile>();
            var tempProfile = new ServantProfile();
            var tempHolder = new SkillHolder();
            PropertyInfo pi;
            var objProps = typeof(ServantProfile).GetProperties().Select(p => p.Name.ToLower());

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    #region big switch
                    switch (reader.Value.ToString())
                    {
                        case nameof(SkillHolder.skill1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.skill1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.rank1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.rank1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect1RankUp):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect1RankUp = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.skill2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.skill2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.rank2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.rank2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect2RankUp):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect2RankUp = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.skill3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.skill3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.rank3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.rank3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.effect3RankUp):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.effect3RankUp = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveSkill1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveSkill1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveRank1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveRank1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveEffect1):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveEffect1 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveSkill2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveSkill2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveRank2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveRank2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveEffect2):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveEffect2 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveSkill3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveSkill3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveRank3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveRank3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveEffect3):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveEffect3 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveSkill4):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveSkill4 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveRank4):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveRank4 = reader.Value.ToString();
                            }
                            break;
                        case nameof(SkillHolder.passiveEffect4):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempHolder.passiveEffect4 = reader.Value.ToString();
                            }
                            break;
                        case "traits":
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                tempProfile.Traits = reader.Value.ToString().Split(',').ToList();
                            }
                            break;
                        default:
                            string readerValue = reader.Value.ToString().ToLower();
                            if (reader.Read() && objProps.Contains(readerValue))
                            {
                                pi = tempProfile.GetType().GetProperty(readerValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                var convertedValue = Convert.ChangeType(reader.Value, pi.PropertyType);
                                pi.SetValue(tempProfile, convertedValue, null);
                            }
                            break;
                    }
                    #endregion
                }
                else if (reader.TokenType == JsonToken.EndObject && reader.Depth == 1)
                {
                    tempProfile.ActiveSkills = new List<ServantSkill>();
                    if (tempHolder.skill1 != null) tempProfile.ActiveSkills.Add(new ServantSkill { SkillName = tempHolder.skill1, Rank = tempHolder.rank1, Effect = tempHolder.effect1, RankUpEffect = tempHolder.effect1RankUp });
                    if (tempHolder.skill2 != null) tempProfile.ActiveSkills.Add(new ServantSkill { SkillName = tempHolder.skill2, Rank = tempHolder.rank2, Effect = tempHolder.effect2, RankUpEffect = tempHolder.effect2RankUp });
                    if (tempHolder.skill3 != null) tempProfile.ActiveSkills.Add(new ServantSkill { SkillName = tempHolder.skill3, Rank = tempHolder.rank3, Effect = tempHolder.effect3, RankUpEffect = tempHolder.effect3RankUp });

                    tempProfile.PassiveSkills = new List<ServantSkill>();
                    if (tempHolder.passiveEffect1 != null) tempProfile.PassiveSkills.Add(new ServantSkill { SkillName = tempHolder.passiveSkill1, Rank = tempHolder.passiveRank1, Effect = tempHolder.passiveEffect1 });
                    if (tempHolder.passiveEffect2 != null) tempProfile.PassiveSkills.Add(new ServantSkill { SkillName = tempHolder.passiveSkill2, Rank = tempHolder.passiveRank2, Effect = tempHolder.passiveEffect2 });
                    if (tempHolder.passiveEffect3 != null) tempProfile.PassiveSkills.Add(new ServantSkill { SkillName = tempHolder.passiveSkill3, Rank = tempHolder.passiveRank3, Effect = tempHolder.passiveEffect3 });
                    if (tempHolder.passiveEffect4 != null) tempProfile.PassiveSkills.Add(new ServantSkill { SkillName = tempHolder.passiveSkill4, Rank = tempHolder.passiveRank4, Effect = tempHolder.passiveEffect4 });
                    
                    mappedObj.Add(tempProfile);

                    tempProfile = new ServantProfile();
                    tempHolder = new SkillHolder();
                }
            }

            return mappedObj;
        }

        private class SkillHolder
        {
            internal string skill1 { get; set; }
            internal string rank1 { get; set; }
            internal string effect1 { get; set; }
            internal string effect1RankUp { get; set; }
            internal string skill2 { get; set; }
            internal string rank2 { get; set; }
            internal string effect2 { get; set; }
            internal string effect2RankUp { get; set; }
            internal string skill3 { get; set; }
            internal string rank3 { get; set; }
            internal string effect3 { get; set; }
            internal string effect3RankUp { get; set; }
            internal string passiveSkill1 { get; set; }
            internal string passiveRank1 { get; set; }
            internal string passiveEffect1 { get; set; }
            internal string passiveSkill2 { get; set; }
            internal string passiveRank2 { get; set; }
            internal string passiveEffect2 { get; set; }
            internal string passiveSkill3 { get; set; }
            internal string passiveRank3 { get; set; }
            internal string passiveEffect3 { get; set; }
            internal string passiveSkill4 { get; set; }
            internal string passiveRank4 { get; set; }
            internal string passiveEffect4 { get; set; }
        }
    }

}
