using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Discord;
using Discord.Commands;
using Discord.Modules;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace MechHisui.FateGOLib
{
    /// <summary>
    /// FGO Friendcode module.
    /// </summary>
    public class FriendsModule : IModule
    {
        private readonly string _friendcodeConfigPath;
        private readonly Func<Command, User, Channel, bool> _checkFunc;
        private List<FriendData> _friendData = new List<FriendData>();

        /// <summary>
        /// Creates a new instance of a <see cref="FriendsModule"/>.
        /// </summary>
        /// <param name="friendcodeConfigPath">Path to the Friendcode config file.</param>
        /// <param name="checkFunc">A function for checking when the command is allowed to run.</param>
        public FriendsModule(string friendcodeConfigPath, Func<Command, User, Channel, bool> checkFunc)
        {
            if (friendcodeConfigPath == null) throw new ArgumentNullException(nameof(friendcodeConfigPath));
            if (checkFunc == null) throw new ArgumentNullException(nameof(checkFunc));
            if (!File.Exists(friendcodeConfigPath)) throw new FileNotFoundException(message: "File not found.", fileName: nameof(friendcodeConfigPath));

            _friendcodeConfigPath = friendcodeConfigPath;
            _checkFunc = checkFunc;
        }

        void IModule.Install(ModuleManager manager)
        {
            Console.WriteLine("Registering 'Friends'...");
            ReadFriendData();
            //FriendCodes.ReadFriendData(_friendcodeConfigPath);
            manager.Client.GetService<CommandService>().CreateCommand("addfc")
               .AddCheck(_checkFunc)
               .Parameter("code", ParameterType.Required)
               .Parameter("slot", ParameterType.Required)
               .Parameter("servant", ParameterType.Optional)
               .Description("Add your friendcode to the list. Enter your code with quotes as `\"XXX XXX XXX\"`. You may optionally add your support Servant as well. If you do, enclose that in `\"\"`s as well.")
               .Do(async cea =>
               {
                   if (_friendData.Any(fc => fc.User == cea.User.Name
                           && fc.Class.Equals(cea.Args[1], StringComparison.OrdinalIgnoreCase)))
                   {
                       await cea.Channel.SendMessage($"Already in the Friendcode list. Please use `.updatefc` to update your description.");
                       return;
                   }

                   SupportClass support;
                   if (!Enum.TryParse(cea.GetArg("class"), true, out support))
                   {
                       await cea.Channel.SendMessage("Could not parse `class` parameter as valid suport slot.");
                       return;
                   }

                   if (Regex.Match(cea.Args[0], @"[0-9][0-9][0-9] [0-9][0-9][0-9] [0-9][0-9][0-9]").Success)
                   {
                       var friend = new FriendData
                       {
                           Id = _friendData.Count + 1,
                           User = cea.User.Name,
                           FriendCode = cea.Args[0],
                           Class = support.ToString(),
                           Servant = (cea.Args.Length > 2) ? cea.Args[2] : String.Empty
                       };
                       _friendData.Add(friend);
                       WriteFriendData();
                       //FriendCodes.WriteFriendData(_friendcodeConfigPath);
                       await cea.Channel.SendMessage($"Added {support.ToString()} support for `{friend.User}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage($"Incorrect friendcode format specified.");
                   }
               });

            manager.Client.GetService<CommandService>().CreateCommand("listfcs")
               .AddCheck(_checkFunc)
               .Parameter("filter", ParameterType.Required)
               .Description("Display known friendcodes.")
               .Do(async cea =>
               {
                   var data = Enumerable.Empty<FriendData>();
                   SupportClass support;
                   if (Enum.TryParse(cea.Args[0], true, out support))
                   {
                       data = _friendData.Where(f => f.Class == support.ToString());
                   }
                   else
                   {
                       await cea.Channel.SendMessage("Could not parse parameter as a valid filter.");
                       return;
                   }

                   var sb = new StringBuilder("```\n");
                   int longestName = data.OrderByDescending(f => f.User.Length).First().User.Length;
                   foreach (var friend in data.OrderBy(f => f.Id))
                   {
                       var spaces = new string(' ', (longestName - friend.User.Length) + 1);
                       sb.Append($"{friend.User}:{spaces}{friend.FriendCode}");
                       sb.AppendLine((!String.IsNullOrEmpty(friend.Servant)) ? $" - {friend.Servant}" : String.Empty);
                       if (sb.Length > 1700)
                       {
                           sb.Append("\n```");
                           await cea.Channel.SendMessage(sb.ToString());
                           sb = sb.Clear().AppendLine("```");
                       }
                   }
                   sb.Append("\n```");
                   await cea.Channel.SendMessage(sb.ToString());
               });

            manager.Client.GetService<CommandService>().CreateCommand("updatefc")
               .AddCheck(_checkFunc)
               .Parameter("slot", ParameterType.Required)
               .Parameter("newServant", ParameterType.Required)
               .Description("Update the Support Servant displayed in your friendcode listing.")
               .Do(async cea =>
               {
                   SupportClass support;
                   if (!Enum.TryParse(cea.GetArg("class"), true, out support))
                   {
                       await cea.Channel.SendMessage("Could not parse `class` parameter as valid support slot.");
                       return;
                   }

                   Func<FriendData, bool> pred = c => c.User == cea.User.Name && c.Class == support.ToString();
                   if (_friendData.Any(pred))
                   {
                       var temp = _friendData.Single(pred);
                       _friendData.Remove(_friendData.Single(pred));
                       temp.Class = support.ToString();
                       temp.Servant = cea.GetArg("newServant");
                       _friendData.Add(temp);
                       WriteFriendData();
                       //FriendCodes.WriteFriendData(_friendcodeConfigPath);
                       await cea.Channel.SendMessage($"Updated `{temp.User}`'s {support.ToString()} Suppport Servant to be `{temp.Servant}`.");
                   }
                   else
                   {
                       await cea.Channel.SendMessage("Profile not found. Please add your profile using `.addfc`.");
                   }
               });
        }

        private void ReadFriendData()
            => _friendData = JsonConvert.DeserializeObject<List<FriendData>>(File.ReadAllText(_friendcodeConfigPath));

        private void WriteFriendData()
            => File.WriteAllText(_friendcodeConfigPath, JsonConvert.SerializeObject(_friendData));

        private enum SupportClass
        {
            Omni,
            Saber,
            Archer,
            Lancer,
            Rider,
            Caster,
            Assassin,
            Berserker
        }
    }
}
