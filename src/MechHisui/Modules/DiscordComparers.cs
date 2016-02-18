using System.Collections.Generic;
using Discord;

namespace DiscordExt
{
    public class UserComparer : Comparer<User>
    {
        public override int Compare(User x, User y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return x.Id.CompareTo(y.Id);
        }
    }

    public class ServerComparer : Comparer<Server>
    {
        public override int Compare(Server x, Server y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return x.Id.CompareTo(y.Id);
        }
    }

    public class ChannelComparer : Comparer<Channel>
    {
        public override int Compare(Channel x, Channel y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return x.Id.CompareTo(y.Id);
        }
    }

    public class MessageComparer : Comparer<Message>
    {
        public override int Compare(Message x, Message y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return x.Id.CompareTo(y.Id);
        }
    }

    public class RoleComparer : Comparer<Role>
    {
        public override int Compare(Role x, Role y)
        {
            if (x == null && y == null) return 0;
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            return x.Id.CompareTo(y.Id);
        }
    }
}
