using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using System.IO;

namespace MechHisui.Modules
{
    public static class FriendCodes
    {
        internal static List<FriendData> friendData = new List<FriendData>();

        internal static void ReadFriendData(string path)
        {
            using (TextReader tr = new StreamReader(path))
            {
                friendData = JsonConvert.DeserializeObject<List<FriendData>>(tr.ReadToEnd()) ?? new List<FriendData>();
            }
            
        }

        internal static void WriteFriendData(string path)
        {
            using (TextWriter tw = new StreamWriter(path))
            {
                tw.Write(JsonConvert.SerializeObject(friendData));
            }
        }

    }

    public class FriendData
    {
        public string User { get; set; }
        public string FriendCode { get; set; }
        public string Servant { get; set; }
    }
}
