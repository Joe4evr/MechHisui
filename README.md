# MechHisui
Mech-Hisui is a bot for the [Discord](http://discordapp.com) chat service, made with [Discord.Net](https://github.com/RogueException/Discord.Net).

This project requires Visual Studio 2015 and the ASP.NET 5 tooling to run.
Once opened, right-click the the project-file in Solution Explorer and click Manage User Secrets and fill in the blanks to configure:
```js
{
    "Email": "...", //bot account login
    "Password": "...", //bot account password
    "channelID1": "...", //Channel ID of interested channel
    "channelID2": "...", //etc.
    "Hello": "...", //hello message
    "Goodbye": "...", //goodbye message
    "Owner": "..." //User ID of owner
}
```
