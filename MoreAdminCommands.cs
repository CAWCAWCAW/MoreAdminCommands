﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Threading;

namespace MoreAdminCommands
{
    [ApiVersion(1, 14)]
    public class MAC : TerrariaPlugin
    {
        public static MACconfig config { get; set; }
        public static string savePath { get { return Path.Combine(TShock.SavePath, "MoreAdminCommands.json"); } }
        public static List<Mplayer> Players = new List<Mplayer>();

        private DateTime LastCheck = DateTime.UtcNow;
        private DateTime OtherLastCheck = DateTime.UtcNow;

        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;

        public static double timeToFreezeAt = 1000;
        public int viewAllTeam = 4;

        public static bool timeFrozen = false;
        public static bool cansend = false;
        public static bool freezeDayTime = true;
        public static bool muteAll = false;

        public override string Name
        {
            get { return "MoreAdminCommands"; }
        }

        public override string Author
        {
            get { return "Created by DaGamesta, Maintained by WhiteX & aMoka"; }
        }

        public override string Description
        {
            get { return "Variety of commands to extend abilities on TShock"; }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        #region Initialize Dispose
        public override void Initialize()
        {
            var Hook = ServerApi.Hooks;

            Hook.GameInitialize.Register(this, OnInitialize);
            Hook.GameUpdate.Register(this, OnUpdate);
            Hook.ServerChat.Register(this, OnChat);
            Hook.NetSendData.Register(this, OnSendData);
            Hook.NetGreetPlayer.Register(this, OnJoin);
            Hook.ServerLeave.Register(this, OnLeave);
            Hook.NetGetData.Register(this, OnGetData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var Hook = ServerApi.Hooks;

                Hook.GameInitialize.Deregister(this, OnInitialize);
                Hook.GameUpdate.Deregister(this, OnUpdate);
                Hook.ServerChat.Deregister(this, OnChat);
                Hook.NetSendData.Deregister(this, OnSendData);
                Hook.NetGreetPlayer.Deregister(this, OnJoin);
                Hook.ServerLeave.Deregister(this, OnLeave);
                Hook.NetGetData.Deregister(this, OnGetData);
            }

            base.Dispose(disposing);
        }
        #endregion

        public MAC(Main game)
            : base(game)
        {
            Order = -1;

            config = new MACconfig();
        }

        #region OnInitialize
        public void OnInitialize(EventArgs args)
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            var table = new SqlTable("muteList",
                        new SqlColumn("Name", MySqlDbType.Text),
                        new SqlColumn("IP", MySqlDbType.Text));
            SQLWriter.EnsureExists(table);

            #region Commands
            Commands.ChatCommands.Add(new Command("mac.kill", Cmds.KillAll, "killall", "kill*"));
            Commands.ChatCommands.Add(new Command("mac.kill", Cmds.AutoKill, "autokill"));
            Commands.ChatCommands.Add(new Command("mac.mute", Cmds.PermaMute, "permamute"));
            Commands.ChatCommands.Add(new Command("mac.mute", Cmds.MuteAll, "muteall"));
            Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnMobPlayer, "spawnmobplayer", "smp"));
            //Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnGroup, "spawngroup", "sg"));
            Commands.ChatCommands.Add(new Command("mac.spawn", Cmds.SpawnByMe, "spawnbyme", "sbm"));
            Commands.ChatCommands.Add(new Command("mac.search", Cmds.FindPerms, "findperm"));
            Commands.ChatCommands.Add(new Command("mac.search", Cmds.FindCommand, "findcommand", "findcmd"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherAll, "butcherall", "butcher*"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherFriendly, "butcherfriendly", "butcherf"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherNPC, "butchernpc"));
            Commands.ChatCommands.Add(new Command("mac.butcher", Cmds.ButcherNear, "butchernear"));
            Commands.ChatCommands.Add(new Command("mac.heal", Cmds.AutoHeal, "autoheal"));
            Commands.ChatCommands.Add(new Command("mac.moon", Cmds.MoonPhase, "moon"));
            Commands.ChatCommands.Add(new Command("mac.give", Cmds.ForceGive, "forcegive"));
            Commands.ChatCommands.Add(new Command("mac.view", Cmds.ViewAll, "view"));
            Commands.ChatCommands.Add(new Command("mac.ghost", Cmds.Ghost, "ghost"));
            Commands.ChatCommands.Add(new Command("mac.reload", Cmds.ReloadMore, "reloadmore"));
            Commands.ChatCommands.Add(new Command("mac.freeze", Cmds.FreezeTime, "freezetime", "ft"));
            Commands.ChatCommands.Add(new Command(Cmds.TeamUnlock, "teamunlock"));
            #endregion

            Utils.SetUpConfig();
        }
        #endregion

        #region OnJoin
        public void OnJoin(GreetPlayerEventArgs args)
        {
            Players.Add(new Mplayer(args.Who));

            var player = TShock.Players[args.Who];
            var Mplayer = Utils.GetPlayers(args.Who);

            var readTableIP = SQLEditor.ReadColumn("muteList", "IP", new List<SqlValue>());

            if (readTableIP.Contains(player.IP))
            {
                Mplayer.muted = true;
                Mplayer.muteTime = -1;
                foreach (TSPlayer tsplr in TShock.Players)
                {
                    if ((tsplr.Group.HasPermission("mute")) || (tsplr.Group.Name == "superadmin"))
                    {
                        tsplr.SendInfoMessage("A player that is on the perma-mute list is about to enter the server, and has been muted.");
                    }
                }
            }
            else
            {
                Mplayer.muteTime = -1;
                Mplayer.muted = false;
            }
        }
        #endregion

        #region OnLeave
        private void OnLeave(LeaveEventArgs args)
        {
            var player = Utils.GetPlayers(args.Who);

            player.isGhost = false;
            player.isHeal = false;
            player.muted = false;
            player.viewAll = false;
            player.accessRed = false;
            player.accessBlue = false;
            player.accessGreen = false;
            player.accessYellow = false;
            player.autoKill = false;

            Players.RemoveAll(pl => pl.Index == args.Who);
        }
        #endregion

        #region GetData
        void OnGetData(GetDataEventArgs e)
        {
            #region PlayerHP
            if (e.MsgID == PacketTypes.PlayerHp)
            {
                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var playerID = reader.ReadByte();
                    var HP = reader.ReadInt16();
                    var MaxHP = reader.ReadInt16();

                    var player = Utils.GetPlayers((int)playerID);

                    if (player.isHeal)
                    {
                        if (HP <= MaxHP / 2)
                        {
                            player.TSPlayer.Heal(500);
                            player.TSPlayer.SendSuccessMessage("You just got healed!");
                        }
                    }
                }
            }
            #endregion

            #region PlayerMana
            //else if (e.MsgID == PacketTypes.PlayerMana)
            //{

            //    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            //    {
            //        var reader = new BinaryReader(data);
            //        var playerID = reader.ReadByte();
            //        var Mana = reader.ReadInt16();
            //        var MaxMana = reader.ReadInt16();

            //        var player = Utils.GetPlayers((int)playerID);

            //        if (player.isHeal)
            //        {
            //            Item star = TShockAPI.TShock.Utils.GetItemById(184);
            //            if (Mana <= MaxMana / 2)
            //            {
            //                TShock.Players[playerID].GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
            //                player.TSPlayer.SendSuccessMessage("Your mana has been restored!!");
            //            }
            //        }
            //    }
            //}
            #endregion

            #region PlayerDamage
            else if (e.MsgID == PacketTypes.PlayerDamage)
            {
                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var ply = reader.ReadByte();
                    var hitDirection = reader.ReadByte();
                    var damage = reader.ReadInt16();


                    if ((damage > config.maxDamage || damage < 0) && !TShock.Players[e.Msg.whoAmI].Group.HasPermission("ignorecheatdetection") && e.Msg.whoAmI != ply)
                    {
                        if (config.maxDamageBan)
                        {
                            TShockAPI.TShock.Utils.Ban(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                        }
                        else if (config.maxDamageKick)
                        {
                            TShockAPI.TShock.Utils.Kick(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                        }
                        if (config.maxDamageIgnore)
                        {
                            e.Handled = true;
                        }

                    }
                    //if (viewAll[ply])
                    //{
                    //    e.Handled = true;         //Should remove invincibility while /view'ing people
                    //}
                }
            }
            #endregion

            #region NPCStrike
            else if (e.MsgID == PacketTypes.NpcStrike)
            {
                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var npcID = reader.ReadInt16();
                    var damage = reader.ReadInt16();
                    if ((damage > config.maxDamage || damage < 0) && !TShock.Players[e.Msg.whoAmI].Group.HasPermission("ignorecheatdetection"))
                    {

                        if (config.maxDamageBan)
                        {
                            TShockAPI.TShock.Utils.Ban(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                        }
                        else if (config.maxDamageKick)
                        {
                            TShockAPI.TShock.Utils.Kick(TShock.Players[e.Msg.whoAmI], "You have exceeded the max damage limit.");
                        }
                        if (config.maxDamageIgnore)
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
            #endregion

            #region PlayerTeam
            else if (e.MsgID == PacketTypes.PlayerTeam)
            {

                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var ply = reader.ReadByte();
                    var team = reader.ReadByte();

                    var player = Utils.GetPlayers((int)ply);

                    switch (team)
                    {

                        case 1:
                            if (config.redPass != "")
                            {
                                if ((!player.accessRed) && (TShock.Players[ply].Group.Name != "superadmin"))
                                {
                                    e.Handled = true;
                                    TShock.Players[ply].SendMessage("This team is locked, use /teamunlock red [password] to access it.", Color.Red);
                                    TShock.Players[ply].SetTeam(TShock.Players[ply].Team);
                                }
                            }
                            break;

                        case 2:
                            if (config.greenPass != "")
                            {
                                if ((!player.accessGreen) && (TShock.Players[ply].Group.Name != "superadmin"))
                                {
                                    e.Handled = true;
                                    TShock.Players[ply].SendMessage("This team is locked, use /teamunlock green [password] to access it.", Color.Red);
                                    TShock.Players[ply].SetTeam(TShock.Players[ply].Team);
                                }
                            }
                            break;

                        case 3:
                            if (config.bluePass != "")
                            {
                                if ((!player.accessBlue) && (TShock.Players[ply].Group.Name != "superadmin"))
                                {
                                    e.Handled = true;
                                    TShock.Players[ply].SendMessage("This team is locked, use /teamunlock blue [password] to access it.", Color.Red);
                                    TShock.Players[ply].SetTeam(TShock.Players[ply].Team);
                                }
                            }
                            break;

                        case 4:
                            if (config.yellowPass != "")
                            {
                                if ((!player.accessYellow) && (TShock.Players[ply].Group.Name != "superadmin"))
                                {
                                    e.Handled = true;
                                    TShock.Players[ply].SendMessage("This team is locked, use /teamunlock yellow [password] to access it.", Color.Red);
                                    TShock.Players[ply].SetTeam(TShock.Players[ply].Team);
                                }
                            }
                            break;
                    }
                }
            }
            #endregion
        }
        #endregion

        #region SendData
        public void OnSendData(SendDataEventArgs e)
        {
            try
            {
                List<int> ghostIDs = new List<int>();
                foreach (Mplayer player in Players)
                {
                    if (player.isGhost)
                        ghostIDs.Add(player.Index);
                }

                switch (e.MsgId)
                {
                    case PacketTypes.DoorUse:
                    case PacketTypes.EffectHeal:
                    case PacketTypes.EffectMana:
                    case PacketTypes.PlayerDamage:
                    case PacketTypes.Zones:
                    case PacketTypes.PlayerAnimation:
                    case PacketTypes.PlayerTeam:
                    case PacketTypes.PlayerSpawn:
                        {
                            if ((ghostIDs.Contains(e.number)) && (Utils.GetPlayers(e.number).isGhost))
                            {
                                e.Handled = true;
                            }
                        }
                        break;

                    case PacketTypes.ProjectileNew:
                    case PacketTypes.ProjectileDestroy:
                        {
                            if ((ghostIDs.Contains(e.ignoreClient)) && (Utils.GetPlayers(e.ignoreClient).isGhost))
                                e.Handled = true;
                        }
                        break;

                    default: break;
                }

                if ((e.number >= 0) && (e.number <= 255) && (Utils.GetPlayers(e.number).isGhost))
                {
                    if ((!cansend) && (e.MsgId == PacketTypes.PlayerUpdate))
                    {
                        e.Handled = true;
                    }
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region OnUpdate
        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
            {
                LastCheck = DateTime.UtcNow;
                if (timeFrozen)
                {
                    if (Main.dayTime != freezeDayTime)
                    {
                        if (timeToFreezeAt > 10000)
                        {
                            timeToFreezeAt -= 100;
                        }
                        else
                        {
                            timeToFreezeAt += 100;
                        }
                    }
                    TSPlayer.Server.SetTime(freezeDayTime, timeToFreezeAt);
                }

                foreach (Mplayer player in Players)
                {

                    if (player.autoKill)
                    {
                        player.TSPlayer.DamagePlayer(9999);
                    }

                    if (player.viewAll)
                    {
                        foreach (TSPlayer tply in TShock.Players)
                        {
                            try
                            {
                                int prevTeam = Main.player[tply.Index].team;
                                Main.player[tply.Index].team = viewAllTeam;
                                NetMessage.SendData((int)PacketTypes.PlayerTeam, player.Index, -1, "", tply.Index);
                                Main.player[tply.Index].team = prevTeam;

                            }
                            catch (Exception) { }
                        }
                    }

                    if (player.muted)
                    {
                        if (player.muteTime > 0)
                        {
                            player.muteTime -= 1;

                            if (player.muteTime <= 0)
                            {
                                player.muted = false;
                                player.muteTime = -1;

                                player.TSPlayer.SendSuccessMessage("Your mute has run out, and you're free to talk again");
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            var Mplayer = Utils.GetPlayers(args.Who);
            if (findIfPlayingCommand(args.Text) && !TShock.Players[args.Who].Group.HasPermission("ghostmode"))
            {
                string sb = "";
                foreach (TSPlayer player in TShock.Players)
                {
                    var ply = Utils.GetPlayers(player.Index);
                    if (player != null && player.Active && !ply.isGhost)
                    {
                        if (sb.Length != 0)
                        {
                            sb += ", ";
                        }
                        sb += player.Name;
                    }
                }
                TShock.Players[args.Who].SendMessage(string.Format("Current players: {0}.", sb), 255, 240, 20);
                args.Handled = true;
            }

            if (((Mplayer.muted) && (findIfMeCommand(args.Text))) || ((muteAll) && (!TShock.Players[args.Who].Group.HasPermission("mute"))))
            {
                TShock.Players[args.Who].SendMessage("You cannot use the /me command, you are muted.", Color.Red);
                args.Handled = true;
                return;
            }

            if (args.Text.StartsWith("/tp "))
            {
                string tempText = args.Text;
                tempText = tempText.Remove(0, 1);
                parseParameters(tempText);
            }

            if ((Mplayer.muted || muteAll) && !TShock.Players[args.Who].Group.HasPermission("mute"))
            {
                var tsplr = TShock.Players[args.Who];
                if (args.Text.StartsWith("/"))
                {
                    Commands.HandleCommand(tsplr, args.Text);
                }
                else
                {
                    if (!muteAll)
                    {
                        if (Mplayer.muteTime <= 0)
                        {
                            tsplr.SendMessage("You have been muted by an admin.", Color.Red);
                        }
                        else
                        {
                            tsplr.SendMessage("You have " + Mplayer.muteTime + " seconds left of muting.", Color.Red);
                        }
                    }
                    else
                    {
                        tsplr.SendMessage("The server is now muted for this reason: " + config.muteAllReason, Color.Red);
                    }
                }
                args.Handled = true;
            }
        }
        #endregion

        #region Stuff
        //public static int distance(Vector2 point1, Point point2)
        //{
        //    return (Convert.ToInt32(Math.Sqrt(Math.Pow(point1.X - point2.X, 2)
        //        + Math.Pow(point1.Y - point2.Y, 2))));
        //}

        public static bool findIfPlayingCommand(string text)
        {

            if (text.StartsWith("/playing"))
            {

                if (text.Length == 8)
                    return true;
                else if (text[8] == ' ')
                    return true;
                else
                    return false;

            }
            else if (text.StartsWith("/who"))
            {

                if (text.Length == 4)
                    return true;
                else if (text[4] == ' ')
                    return true;
                else
                    return false;

            }
            else
                return false;

        }
        public static bool findIfMeCommand(string text)
        {

            if (text.StartsWith("/me"))
            {

                if (text.Length == 3)
                    return true;
                else if (text[3] == ' ')
                    return true;
                else
                    return false;

            }
            else
                return false;

        }
        public static List<String> parseParameters(string str) {

            var ret = new List<string>();
            string sb = "";
            bool instr = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (instr)
                {
                    if (c == '\\')
                    {
                        if (i + 1 >= str.Length)
                            break;
                        c = GetEscape(str[++i]);
                    }
                    else if (c == '"')
                    {
                        ret.Add(sb);
                        sb = "";
                        instr = false;
                        continue;
                    }
                    sb += c;
                }
                else
                {
                    if (IsWhiteSpace(c))
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb = "";
                        }
                    }
                    else if (c == '"')
                    {
                        if (sb.Length > 0)
                        {
                            ret.Add(sb.ToString());
                            sb = "";
                        }
                        instr = true;
                    }
                    else
                    {
                        sb += c;
                    }
                }
            }
            if (sb.Length > 0)
                ret.Add(sb.ToString());

            return ret;

        }
        private static char GetEscape(char c)
        {
            switch (c)
            {
                case '\\':
                    return '\\';
                case '"':
                    return '"';
                case 't':
                    return '\t';
                default:
                    return c;
            }
        }
        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }
        #endregion

        #region SearchTable
        public static int SearchTable(List<object> Table, string Query)
        {
            for (int i = 0; i < Table.Count; i++)
            {
                try
                {
                    if (Query == Table[i].ToString())
                    {
                        return (i);
                    }
                }
                catch { }
            }
            return (-1);
        }
        #endregion
    }
}