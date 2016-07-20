using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Modules;

namespace MechHisui.Modules
{
    public class UpdateModule : IModule
    {
        private readonly Dictionary<string, Func<CommandEventArgs, Task>> updateDict = new Dictionary<string, Func<CommandEventArgs, Task>>();

        public void Register(string paramName, Func<CommandEventArgs, Task> fn)
        {
            updateDict.Add(paramName, fn);
        }

        void IModule.Install(ModuleManager manager)
        {
            updateDict.Add("all", async e =>
            {
                foreach (var entry in updateDict.Where(kv => kv.Key != "all"))
                {
                    await entry.Value?.Invoke(e);
                }

                await e.Channel.SendWithRetry("Updated all updatables.");
            });

            manager.Client.GetService<CommandService>().CreateCommand("update")
                .Parameter("param")
                .Do(async cea =>
                {
                    Func<CommandEventArgs, Task> fn;
                    if (updateDict.TryGetValue(cea.Args[0], out fn))
                    {
                        await fn?.Invoke(cea);
                    }
                });
        }
    }
}
