# GenericBot: An All-Purpose Almost-Decent Bot
[![Build status](https://mastrchef.visualstudio.com/GenericBot/_apis/build/status/GenericBot-ASP.NET%20Core-CI)](https://mastrchef.visualstudio.com/GenericBot/_build/latest?definitionId=1)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fgalenguyer%2FGenericBot.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fgalenguyer%2FGenericBot?ref=badge_shield)

GenericBot aims to provide an almost full featured moderation and fun box experience in one convenient package

GenericBot is, and always will be, free to use. To invite it, click [here](https://discordapp.com/oauth2/authorize?client_id=295329346590343168&scope=bot&permissions=2110258303)

### Getting Started
See everything you can make me do with >help. Admins can also run >configinfo to see everything you can set up

### Self Assignable Roles
One of the most popular features GenericBot is used for is roles a user can assign to themself. To see all the avaible roles, do >userroles. You can join a role with >iam [rolename] or leave a role with >iamnot [rolename].

### Moderation
GenericBot provides a wide range of tools for moderators to track users and infractions. It keeps track of all of a user's usernames, nicknames, and logged infractions, including kicks and timed or permanent bans. Users can be searched for either by ID, or by username or nickname, whether it be current or an old name. (All data is stored in an encrypted database, and data from one server is completely inaccessible by another server)

### Fun!
In addition to being a highly effective moderator toolkit, GenericBot has some fun commands, such as >dog, >cat, or >jeff. You can also create your own custom commands for rapid-fire memery or whatever else tickles your fancy

### Development 
GenericBot is constantly in active development. 

#### Issue Tracking
If you notice a bug or want a feature added, open an [issue!](https://github.com/MasterChief-John-117/GenericBot/issues) I'll get to it as soon as I can. 

#### V3 Re-Write
In late September, I took it upon myself to completely re-write GenericBot. I've added some features, removed some others, and generally streamlined the code to make it maintainble. 

##### Things I've added
- Better error handling. Any error sends a message via webhook to a channel I get notifications for, so I can track down and diagnose issues faster than ever before. If the error is seen 5 times, an issue is created on Github.
- Better database abstraction. Any access to the database goes through a wrapper I wrote to ensure DRY. Previously, the database was accessed in different ways depending on when I wrote that code. 
- Improved command context. Previously, the global client, the message itself, and an array of parameters was passed in. Now a unified object is given to the command, improving readability and simplifying data access. Furthermore, now newlines can be preserved. 
- Some commands work in DMs now! This was enabled by the improved context. Previously, messages had to be used in a server for the object to be constructed properly, but with the new context the server can be null, and if a flag is enabled on the individual command, the bot will allow the command to execute in DMs. 
- Audit log for Moderator level commands. The log can only be added to, there's no method to remove them exposed by the bot. This ensures if a moderator goes rogue you can see who it was and exactly what they did.
- General bug fixes. Small things like unbans not working if the user wasn't banned by the bot or Administrator permissions not being deteted that were.

##### Things I've removed
- Points have been removed, at least for the time being. The code around them was very messy and doesn't work with the new structure. 


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fgalenguyer%2FGenericBot.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fgalenguyer%2FGenericBot?ref=badge_large)