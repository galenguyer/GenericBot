using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GenericBot.Entities
{
    public class GuildConfig
    {
        [Flags]
        public enum AntiSpamLevel
        {
            None = 1,
            Basic = 2, 
            Advanced = 4, /* Username filtering */
            Aggressive = 8,
            ActiveRaid = 16
        }
        public ulong GuildId;
        public List<ulong> AdminRoleIds;
        public List<ulong> ModRoleIds;
        public List<ulong> UserRoleIds;
        public List<ulong> AutoRoleIds;
        public ulong UserLogChannelId;
        public List<ulong> MessageLoggingIgnoreChannels = new List<ulong>();
        public ulong VerifiedRole = 0;
        public string VerifiedMessage;
        public AntiSpamLevel AntispamLevel = GuildConfig.AntiSpamLevel.None;

        public string PointsName = "point";
        public string PointsVerb = "used";
        public bool PointsEnabled = false;
        public Dictionary<decimal, ulong> Levels = new Dictionary<decimal, ulong>();

        public bool GlobalBanOptOut = false;

        public ulong FourChannelId;
        public string Prefix;
        public bool AllowTwitter = false;
        public Giveaway Giveaway;

        public List<ChannelMute> ChannelMutes = new List<ChannelMute>();
        public List<CustomCommand> CustomCommands;
        public List<CustomAlias> CustomAliases;

        public List<ulong> ProbablyMutedUsers = new List<ulong>();
        public ulong MutedRoleId = 0;
        public List<GenericBan> Bans = new List<GenericBan>();
        public Dictionary<string, string> ReplacementEmojis;

        public Dictionary<ulong, Discord.OverwritePermissions> ChannelOverrideDefaults = new Dictionary<ulong, Discord.OverwritePermissions>();

        public GuildConfig(ulong id)
        {
            GuildId = id;
            AdminRoleIds = new List<ulong>();
            ModRoleIds = new List<ulong>();
            UserRoleIds = new List<ulong>();
            AutoRoleIds = new List<ulong>();
            CustomCommands = new List<CustomCommand>();
            CustomAliases = new List<CustomAlias>();
            ReplacementEmojis = new Dictionary<string, string>();
            
            Bans = new List<GenericBan>();

            Prefix = "";
        }

        public GuildConfig Save()
        {
            if (!GenericBot.GuildConfigs.Any(kvp => kvp.Key.Equals(GuildId)))
            {
                GenericBot.GuildConfigs.Add(GuildId, this);
            }
            else
            {
                GenericBot.GuildConfigs[GuildId] = this;
            }
            File.WriteAllText($"files/guildConfigs/{this.GuildId}.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            return this;
        }

        public bool AddBan(GenericBan ban)
        {
            try
            {
                var guild = GenericBot.DiscordClient.GetGuild(GuildId);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }

    public class GenericBan
    {
        public ulong Id;
        public ulong GuildId;
        public DateTimeOffset BannedUntil;
        public string Reason;

        public GenericBan(ulong userid, ulong guildid, string reason, int days = 0)
        {
            this.Id = userid;
            this.GuildId = guildid;
            this.BannedUntil = days != 0 ? DateTimeOffset.UtcNow + TimeSpan.FromDays(days) : DateTimeOffset.MaxValue;
            this.Reason = reason;
        }
    }
}
