    //using System;
    //using System.Collections.Generic;
    //using System.Linq;
    //using System.Threading.Tasks;
    //using Discord;
    //using System.IO;
    //using Microsoft.Extensions.Configuration;
    //using System.Text;

    //namespace MechHisui.Modules
    //{
    //    public class Recorder
    //    {
    //        public Channel channel { get; }

    //        private readonly TextWriter writer;

    //        public Recorder(Channel channel, DiscordClient client, IConfiguration config)
    //        {
    //            this.channel = channel;
    //            writer = new StreamWriter(Path.GetFullPath($"{config["Recordings"]}{channel.Server.Name} - {channel.Name} - {DateTime.UtcNow.Date}.txt"), true, Encoding.UTF8);
    //            client.MessageReceived += LogToFile;
    //            channel.SendMessage($"Recording in {channel.Name}....").GetAwaiter().GetResult();
    //        }

    //        public async void LogToFile(object sender, MessageEventArgs e)
    //        {
    //            if (Helpers.IsWhilested(e.Channel, (DiscordClient)sender))
    //            {
    //                await writer.WriteLineAsync($"{e.Message.Timestamp.ToUniversalTime()} - {e.Message.User.Name}\t\t: {e.Message.Text}");
    //            }
    //        }

    //        public async Task EndRecord(DiscordClient client)
    //        {
    //            await channel.SendMessage($"Stopped recording in {channel.Name}.");
    //            client.MessageReceived -= LogToFile;
    //            await writer.FlushAsync();
    //            writer.Dispose();
    //        }
    //    }
    //}
