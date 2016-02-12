﻿using System;
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
            var activeSkills = new List<ServantSkill>();
            var passiveSkills = new List<ServantSkill>();
            var traits = new List<string>();
            var tempProfile = new ServantProfile();
            var tempHolder = new SkillHolder();
            var objProps = typeof(ServantProfile).GetProperties().Select(p => p.Name.ToLower()).ToArray();
            PropertyInfo pi;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
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
                        case nameof(traits):
                            if (reader.Read() && reader.TokenType == JsonToken.String)
                            {
                                traits = reader.Value.ToString().Split(',').ToList();
                            }
                            break;
                        default:
                            string readerValue = reader.Value.ToString().ToLower();
                            if (reader.Read())
                            {
                                if (objProps.Contains(readerValue))
                                {
                                    pi = tempProfile.GetType().GetProperty(readerValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                    var convertedValue = Convert.ChangeType(reader.Value, pi.PropertyType);
                                    pi.SetValue(tempProfile, convertedValue, null);
                                }
                            }
                            break;
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject && reader.Depth == 1)
                {
                    activeSkills.AddRange(new[] {
                        new ServantSkill { SkillName = tempHolder.skill1, Rank = tempHolder.rank1, Effect = tempHolder.effect1 },
                        new ServantSkill { SkillName = tempHolder.skill2, Rank = tempHolder.rank2, Effect = tempHolder.effect2 },
                        new ServantSkill { SkillName = tempHolder.skill3, Rank = tempHolder.rank3, Effect = tempHolder.effect3 },
                    });
                    passiveSkills.AddRange(new[]
                    {
                        new ServantSkill { SkillName = tempHolder.passiveSkill1, Rank = tempHolder.passiveRank1, Effect = tempHolder.passiveEffect1 },
                        new ServantSkill { SkillName = tempHolder.passiveSkill2, Rank = tempHolder.passiveRank2, Effect = tempHolder.passiveEffect2 },
                        new ServantSkill { SkillName = tempHolder.passiveSkill3, Rank = tempHolder.passiveRank3, Effect = tempHolder.passiveEffect3 },
                        new ServantSkill { SkillName = tempHolder.passiveSkill4, Rank = tempHolder.passiveRank4, Effect = tempHolder.passiveEffect4 },
                    });

                    tempProfile.ActiveSkills = activeSkills;
                    tempProfile.PassiveSkills = passiveSkills;
                    tempProfile.Traits = traits;
                    mappedObj.Add(tempProfile);

                    activeSkills = new List<ServantSkill>();
                    passiveSkills = new List<ServantSkill>();
                    tempProfile = new ServantProfile();
                    tempHolder = new SkillHolder();
                }
            }
            
            return mappedObj;
        }
    }

    internal class SkillHolder
    {
        internal string skill1 { get; set; }
        internal string rank1 { get; set; }
        internal string effect1 { get; set; }
        internal string skill2 { get; set; }
        internal string rank2 { get; set; }
        internal string effect2 { get; set; }
        internal string skill3 { get; set; }
        internal string rank3 { get; set; }
        internal string effect3 { get; set; }
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