using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using NinjaBotCore.Models.Wow;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using NinjaBotCore.Database;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace NinjaBotCore.Modules.Wow
{
    public class WarcraftLogs
    {
        private static CancellationTokenSource _tokenSource;
        private static List<Zones> _zones;
        private static List<CharClasses> _charClasses;
        private readonly IConfigurationRoot _config;
        private DiscordSocketClient _client;
        private readonly WclApiRequestor _api;

        public WarcraftLogs(IConfigurationRoot config, DiscordSocketClient client, bool throttle = true)
        {
            _client = client;
            _config = config;
            
            try 
            {
                _api = throttle ? new ApiRequesterThrottle(_config["WarcraftLogsApi"]) : new WclApiRequestor(_config["WarcraftLogsApi"]);
                CharClasses = this.GetCharClasses().Result;
                Zones = this.GetZones().Result;
                //this.StartTimer();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error performing class setup for WCL: {ex.Message}");
            }
        }

        public static List<Zones> Zones
        {
            get
            {
                return _zones;
            }
            private set
            {
                _zones = value;
            }
        }

        public static CancellationTokenSource TokenSource
        {
            get
            {
                return _tokenSource;
            }
            set
            {
                _tokenSource = value;
            }
        }

        public static List<CharClasses> CharClasses
        {
            get
            {
                return _charClasses;
            }
            private set
            {
                _charClasses = value;
            }
        }

        public async Task<string> LogsApiRequestAsync(string url, bool isList = false)
        {
            string response   = string.Empty;
            string wowLogsKey = string.Empty;            
            string baseUrl    = "https://www.warcraftlogs.com:443/v1";
            
            if (isList) 
            {
                wowLogsKey = $"api_key={_config["WarcraftLogsApi"]}";
            }
            else
            {
                wowLogsKey = $"api_key={_config["WarcraftLogsApiCmd"]}";
            }
            
            url = $"{baseUrl}{url}{wowLogsKey}";

            Console.WriteLine($"Calling WarcraftLogs API with URL: {url}");

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //test = httpClient.PostAsJsonAsync<FaceRequest>(fullUrl, request).Result;             
                response = await httpClient.GetStringAsync(url);
            }

            return response;            
        }

        public string LogsApiRequest(string url, bool isList = false)
        {
            string response   = string.Empty;
            string wowLogsKey = string.Empty;            
            string baseUrl    = "https://www.warcraftlogs.com:443/v1";

            if (isList) 
            {
                wowLogsKey = $"api_key={_config["WarcraftLogsApi"]}";
            }
            else
            {
                wowLogsKey = $"api_key={_config["WarcraftLogsApiCmd"]}";
            }
            
            url = $"{baseUrl}{url}{wowLogsKey}";

            Console.WriteLine($"Calling WarcraftLogs API with URL: {url}");

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //test = httpClient.PostAsJsonAsync<FaceRequest>(fullUrl, request).Result;             
                response = httpClient.GetStringAsync(url).Result;
            }

            return response;            
        }        

        public async Task<List<CharClasses>> GetCharClasses()
        {
            return await _api.Get<List<CharClasses>>($"classes?api_key={_config["WarcraftLogsApi"]}");
        }

        public async Task<List<Reports>> GetReportsFromGuild(string guildName, string realm, string region, bool isList = false)
        {
            string url = string.Empty;
            string realmSlug = string.Empty;
            switch (region.ToLower())
            {
                case "us":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realm.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "eu":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realm.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }
            System.Console.WriteLine($"SLUG: {realmSlug}");            
            url = $"reports/guild/{guildName.Replace(" ", "%20")}/{realmSlug}/{region}?api_key={_config["WarcraftLogsApi"]}";

            return await _api.Get<List<Reports>>(url);

        }

        public async Task<List<Reports>> GetReportsFromGuild(string guildName, string realm, string locale, string region, bool isList = false)
        {
            string url = string.Empty;
            string realmSlug = string.Empty;
            switch (locale)
            {
                case "en_US":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realm.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "ru_RU":
                    {                    
                        realmSlug = WowApi.RealmInfoRu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realm.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "en_GB":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realm.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }            
            url = $"reports/guild/{guildName.Replace(" ", "%20")}/{realmSlug}/{region}?api_key={_config["WarcraftLogsApi"]}";
         

            return await _api.Get<List<Reports>>(url);
        }

        public async Task<List<Reports>> GetReportsFromGuild(string guildName, string realm, string locale, string region, string realmSlug , bool isList = false)
        {
            string url = string.Empty;         
            url = $"reports/guild/{guildName.Replace(" ", "%20")}/{realmSlug}/{region}?api_key={_config["WarcraftLogsApi"]}";

            return await _api.Get<List<Reports>>(url);
        }

        public async Task<List<Reports>> GetReportsFromUser(string userName)
        {
            string url = string.Empty;
            url = $"/reports/user/{userName.Replace(" ", "%20")}?api_key={_config["WarcraftLogsApi"]}";
            return await _api.Get<List<Reports>>(url);
        }

        public List<CharParses> GetParsesFromCharacterName(string charName, string realm, string region = "us")
        {
            List<CharParses> parse;
            string url = string.Empty;
            //https://www.warcraftlogs.com:443/v1/parses/character/oceanbreeze/thunderlord/US?zone=10&api_key=06cd4398efb2643988e1bb0e1387419a
            url = $"/parses/character/{charName}/{realm}/{region}?";

            parse = JsonConvert.DeserializeObject<List<CharParses>>(LogsApiRequest(url));

            return parse;
        }

        public List<LogCharRankings> GetRankingFromCharName(string charName, string realm, string region = "us")
        {
            List<LogCharRankings> charRankings;
            string url = string.Empty;

            url = $"/rankings/character/{charName}/{realm}/{region}?";

            charRankings = JsonConvert.DeserializeObject<List<LogCharRankings>>(LogsApiRequest(url));

            return charRankings;
        }

        public List<LogCharRankings> GetRankingFromCharName(string charName, string realm, string zone, string region = "us")
        {
            List<LogCharRankings> charRankings;
            string url = string.Empty;
            int zoneID = 0;
            string findString = string.Empty;
            switch (zone)
            {
                case "en":
                    {
                        findString = "Emerald Nightmare";
                        break;
                    }
                case "tov":
                    {
                        findString = "Trial of Valor";
                        break;
                    }
                case "nh":
                    {
                        findString = "The Nighthold";
                        break;
                    }
                case "tos":
                    {
                        findString = "Tomb of Sargeras";
                        break;
                    }
            }
            zoneID = Zones.Where(z => z.name == findString).Select(z => z.id).FirstOrDefault();
            url = $"/rankings/character/{charName}/{realm}/{region}?zone={zoneID}&";
            charRankings = JsonConvert.DeserializeObject<List<LogCharRankings>>(LogsApiRequest(url));
            return charRankings;
        }

        public async Task<List<Zones>> GetZones()
        {
            string url = string.Empty;
            url = $"zones?api_key={_config["WarcraftLogsApi"]}";
         

            return await _api.Get<List<Zones>>(url);
        }

        public Fights GetFights(string code)
        {
            string url = string.Empty;
            Fights fights;
            url = $"/report/fights/{code}?";

            fights = JsonConvert.DeserializeObject<Fights>(LogsApiRequest(url));

            return fights;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounter(int encounterID, string realmName, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            string realmSlug = string.Empty;
            switch (regionName.ToLower())
            {
                case "us":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "eu":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }                        
            string url = $"/rankings/encounter/{encounterID}?metric={metric}&server={realmSlug}&region={regionName}&difficulty={difficulty}&limit=1000&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounter(int encounterID, string realmName, string partition, string realmSlug, string metric = "dps", int difficulty = 4, string regionName = "us")
        {               
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            string url = $"/rankings/encounter/{encounterID}?metric={metric}&server={realmSlug}&region={regionName}&difficulty={difficulty}&limit=1000&partition={partition}&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterSlug(int encounterID, string realmSlug, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();                   
            string url = $"/rankings/encounter/{encounterID}?metric={metric}&server={realmSlug}&region={regionName}&difficulty={difficulty}&limit=1000&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterSlug(int encounterID, string realmSlug, string partition, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();                   
            string url = $"/rankings/encounter/{encounterID}?metric={metric}&server={realmSlug}&region={regionName}&difficulty={difficulty}&limit=1000&partition={partition}&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterGuild(int encounterID, string realmName, string guildName, string partition, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            guildName = guildName.Replace(" ", "%20");
            string realmSlug = string.Empty;
            switch (regionName.ToLower())
            {
                case "us":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "eu":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }
            string url = $"/rankings/encounter/{encounterID}?guild={guildName}&server={realmSlug}&region={regionName}&metric={metric}&difficulty={difficulty}&limit=1000&partition={partition}&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterGuild(int encounterID, string realmName, string guildName, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            guildName = guildName.Replace(" ", "%20");
            string realmSlug = string.Empty;
            switch (regionName.ToLower())
            {
                case "us":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "eu":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }
            string url = $"/rankings/encounter/{encounterID}?guild={guildName}&server={realmSlug}&region={regionName}&metric={metric}&difficulty={difficulty}&limit=1000&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterGuildSlug(int encounterID, string realmSlug, string guildName, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            guildName = guildName.Replace(" ", "%20");
            string url = $"/rankings/encounter/{encounterID}?guild={guildName}&server={realmSlug}&region={regionName}&metric={metric}&difficulty={difficulty}&limit=1000&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public WarcraftlogRankings.RankingObject GetRankingsByEncounterGuildSlug(int encounterID, string realmSlug, string partition, string guildName, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            guildName = guildName.Replace(" ", "%20");
            string url = $"/rankings/encounter/{encounterID}?guild={guildName}&server={realmSlug}&region={regionName}&metric={metric}&difficulty={difficulty}&limit=1000&partition={partition}&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }
        
        public WarcraftlogRankings.RankingObject GetRankingsByEncounter(int encounterID, string realmName, string partition, string metric = "dps", int difficulty = 4, string regionName = "us")
        {
            string realmSlug = string.Empty;
            switch (regionName.ToLower())
            {
                case "us":
                    {
                        realmSlug = WowApi.RealmInfo.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
                case "eu":
                    {
                        realmSlug = WowApi.RealmInfoEu.realms.Where(r => r.name.Replace("'","").ToLower().Contains(realmName.ToLower())).Select(s => s.slug).FirstOrDefault();
                        break;
                    }
            }            
            WarcraftlogRankings.RankingObject l = new WarcraftlogRankings.RankingObject();
            string url = $"/rankings/encounter/{encounterID}?metric={metric}&server={realmSlug}&region={regionName}&difficulty={difficulty}&limit=1000&partition={partition}&";
            l = JsonConvert.DeserializeObject<WarcraftlogRankings.RankingObject>(LogsApiRequest(url));
            return l;
        }

        public DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public async Task WarcraftLogsTimer(Action action, TimeSpan interval, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    action();
                    await Task.Delay(interval, token);
                }
            }
            catch (TaskCanceledException ex)
            {

            }
        }

        public async Task StartTimer()
        {
            TokenSource = new CancellationTokenSource();
            var timerAction = new Action(CheckForNewLogs);
            await WarcraftLogsTimer(timerAction, TimeSpan.FromSeconds(90), TokenSource.Token);
        }

        public async Task StopTimer()
        {
            TokenSource.Cancel();
        }

        async void CheckForNewLogs()
        {
            List<WowGuildAssociations> guildList = null;
            List<LogMonitoring> logWatchList = null;
            try
            {

                using (var db = new NinjaBotEntities())
                {
                    guildList = db.WowGuildAssociations.ToList();
                    logWatchList = db.LogMonitoring.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error getting guild/logwatch list -> [{ex.Message}]");
            }
            if (guildList != null)
            {
                foreach (var guild in guildList)
                {                    
                    try
                    {                        
                        var watchGuild = logWatchList.Where(w => w.ServerId == guild.ServerId).FirstOrDefault();
                        if (watchGuild != null)
                        {
                            if (watchGuild.MonitorLogs)
                            {
                                List<Reports> logs = null;
                                if (!string.IsNullOrEmpty(guild.LocalRealmSlug)) 
                                {

                                    logs = await GetReportsFromGuild(guildName: guild.WowGuild, locale: guild.Locale, realm: guild.WowRealm.Replace("'", ""), realmSlug: guild.LocalRealmSlug, region: guild.WowRegion, isList: true);

                                }
                                else if (!string.IsNullOrEmpty(guild.Locale)) 
                                {          
                                                                        
                                    logs = await GetReportsFromGuild(guildName: guild.WowGuild, realm: guild.WowRealm.Replace("'", ""), region: guild.WowRegion, isList: true, locale: guild.Locale);

                                } else {

                                    logs = await GetReportsFromGuild(guildName: guild.WowGuild,realm: guild.WowRealm.Replace("'", ""), region: guild.WowRegion, isList: true);

                                }
                                if (logs != null)
                                {
                                    var latestLog = logs[0];                                    
                                    DateTime startTime = UnixTimeStampToDateTime(latestLog.start);
                                    if (latestLog.id != watchGuild.ReportId)
                                    {
                                        using (var db = new NinjaBotEntities())
                                        {
                                            var latestForGuild = db.LogMonitoring.Where(l => l.ServerId == guild.ServerId).FirstOrDefault();
                                            latestForGuild.LatestLog = startTime;
                                            latestForGuild.ReportId = latestLog.id;
                                            await db.SaveChangesAsync();
                                        }                                        
                                        ISocketMessageChannel channel = _client.GetChannel((ulong)watchGuild.ChannelId) as ISocketMessageChannel;
                                        if (channel != null)
                                        {
                                            var embed = new EmbedBuilder();
                                            embed.Title = $"New log found for [{guild.WowGuild}]!";
                                            StringBuilder sb = new StringBuilder();
                                            sb.AppendLine($"[__**{latestLog.title}** **/** **{latestLog.zoneName}**__]({latestLog.reportURL})");
                                            sb.AppendLine($"\t:timer: Start time: **{UnixTimeStampToDateTime(latestLog.start)}**");
                                            //sb.AppendLine($"\tLink: ***");
                                            sb.AppendLine($"\t:mag: [WoWAnalyzer](https://wowanalyzer.com/report/{latestLog.id}) | :sob: [WipeFest](https://www.wipefest.net/report/{latestLog.id}) ");
                                            sb.AppendLine();
                                            embed.Description = sb.ToString();
                                            embed.WithColor(new Color(0, 0, 255));
                                            await channel.SendMessageAsync("", false, embed);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error checking for logs [{guild.WowGuild}]:[{guild.WowRealm}]:[{guild.WowRealm}]! -> [{ex.Message}]");
                    }
                }
            }
        }
    }
}