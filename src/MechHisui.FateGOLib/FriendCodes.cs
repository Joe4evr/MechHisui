using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MechHisui.FateGOLib
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
                tw.Write(JsonConvert.SerializeObject(friendData, Formatting.Indented));
            }
        }

    }

    public class FriendData
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string FriendCode { get; set; }
        public string Servant { get; set; }
    }
}
