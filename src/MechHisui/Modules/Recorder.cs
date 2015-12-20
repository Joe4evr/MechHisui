using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace MechHisui.Modules
{
    public class Recorder
    {
        public Channel channel { get; }

        private readonly TextWriter writer;

        public Recorder(Channel channel, DiscordClient client, IConfiguration config)
        {
            this.channel = channel;
            writer = new StreamWriter(Path.GetFullPath($"{config["Recordings"]}{channel.Server.Name} - {channel.Name} - {DateTime.UtcNow.Date}.txt"), true, Encoding.UTF8);
            client.MessageReceived += LogToFile;
            client.SendMessage(channel, $"Recording in {channel}....");
        }

        public async void LogToFile(object sender, MessageEventArgs e)
        {
            if (Helpers.IsWhilested(e.Channel, (DiscordClient)sender))
            {
                await writer.WriteLineAsync($"{e.Message.Timestamp.ToUniversalTime()} - {e.Message.User.Name}\t\t: {e.Message.Text}");
            }
        }

        public async Task EndRecord(DiscordClient client)
        {
            await client.SendMessage(channel, $"Stopped recording in {channel}.");
            client.MessageReceived -= LogToFile;
            await writer.FlushAsync();
            writer.Dispose();
        }
    }
}
