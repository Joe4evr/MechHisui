//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Discord.Commands;
//using Discord.WebSocket;

//namespace DivaBot
//{
//    internal sealed class AttachmentTypeReader : TypeReader
//    {
//        public AttachmentTypeReader(DiscordSocketClient client)
//        {
//            client.MessageUpdated += Client_MessageUpdated;
//        }

//        private Task Client_MessageUpdated(Discord.Optional<SocketMessage> before, SocketMessage after)
//        {
//            Monitor.Pulse(after);
//            return Task.CompletedTask;
//        }

//        public override Task<TypeReaderResult> Read(ICommandContext context, string input)
//        {
//            return (Monitor.Wait(context.Message, 2500) && context.Message.Attachments.Any())
//                ? Task.FromResult(TypeReaderResult.FromSuccess(context.Message.Attachments))
//                : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No attachment on the message."));
//        }
//    }
//}
