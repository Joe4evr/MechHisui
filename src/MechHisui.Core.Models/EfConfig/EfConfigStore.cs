//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Discord.Addons.SimpleConfig;
//using MechHisui.Core.EfConfig.Models;

//namespace MechHisui.Core.EfConfig
//{
//    public sealed class EfConfigStore : IConfigStore<MechHisuiConfig>
//    {
//        private readonly EfConfigContext _db = new EfConfigContext();
//        public MechHisuiConfig Load()
//        {
//            return new MechHisuiConfig(_db);
//        }

//        public void Save(MechHisuiConfig config)
//        {
//            _db.SaveChanges();
//        }

//        //private static ulong SignedToUnsigned64(long value)
//        //    => BitConverter.ToUInt64(BitConverter.GetBytes(value), 0);

//        //private static long UnsignedToSigned64(ulong value)
//        //    => BitConverter.ToInt64(BitConverter.GetBytes(value), 0);
//    }
//}
