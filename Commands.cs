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
    public class Cmds
    {
        #region FindCommand
        public static void FindCommand(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                List<string> commandNameList = new List<string>();

                foreach (Command command in Commands.ChatCommands)
                {
                    for (int i = 0; i < command.Permissions.Count; i++)
                    {
                        if (args.Player.Group.HasPermission(command.Permissions[i]))
                        {
                            foreach (string commandName in command.Names)
                            {
                                bool showCommand = true;
                                foreach (string searchParameter in args.Parameters)
                                {
                                    if (!commandName.Contains(searchParameter))
                                    {
                                        showCommand = false;
                                        break;
                                    }
                                }
                                if (showCommand && !commandNameList.Contains(commandName))
                                {
                                    commandNameList.Add(command.Name);
                                }
                            }
                        }
                    }
                }
                if (commandNameList.Count > 0)
                {
                    args.Player.SendMessage("The following commands matched your search:", Color.Yellow);
                    for (int i = 0; i < commandNameList.Count && i < 6; i++)
                    {
                        string returnLine = "";
                        for (int j = 0; j < commandNameList.Count - i * 5 && j < 5; j++)
                        {
                            if (i * 5 + j + 1 < commandNameList.Count)
                            {
                                returnLine += commandNameList[i * 5 + j] + ", ";
                            }
                            else
                            {
                                returnLine += commandNameList[i * 5 + j] + ".";
                            }
                        }
                        args.Player.SendInfoMessage(returnLine);
                    }
                }
                else
                {
                    args.Player.SendErrorMessage("No Commands matched your search term(s).");
                }
            }
            else
            {
                args.Player.SendErrorMessage("Invalid syntax. Try /findcommand [term 1] (optional[term 2] [term 3] etc)");
            }
        }
        #endregion

        #region FindPermission
        public static void FindPerms(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                foreach (Command cmd in TShockAPI.Commands.ChatCommands)
                {
                    if (cmd.Names.Contains(args.Parameters[0]))
                    {
                        args.Player.SendInfoMessage(string.Format("Permission to use {0}: {1}",
                            cmd.Name, cmd.Permissions[0] != "" ? cmd.Permissions[0] : "Nothing"));
                        return;
                    }
                }
                args.Player.SendErrorMessage("Command not be found.");
            }
            else
            {
                args.Player.SendErrorMessage("Invalid syntax. Try /findperm [command]");
            }
        }
        #endregion

        #region MoonPhase
        public static void MoonPhase(CommandArgs args)
        {
            int phase;
            bool result = Int32.TryParse(args.Parameters[0], out phase);
            if (result && phase > -1 && phase < 8 && args.Parameters.Count > 0)
            {
                string phaseName = "";
                Main.moonPhase = phase;

                #region PhaseName
                switch (phase)
                {
                    case 0:
                        phaseName = "full";
                        break;
                    case 1:
                        phaseName = "3/4";
                        break;
                    case 2:
                        phaseName = "1/2";
                        break;
                    case 3:
                        phaseName = "1/4";
                        break;
                    case 4:
                        phaseName = "new";
                        break;
                    case 5:
                        phaseName = "1/4";
                        break;
                    case 6:
                        phaseName = "1/2";
                        break;
                    case 7:
                        phaseName = "3/4";
                        break;
                }
                #endregion

                TSPlayer.All.SendInfoMessage("Moon phase set to {0}.", phaseName);
            }
            else
                args.Player.SendErrorMessage("Invalid usage! Proper usage: /moon [0-7]");
        }
        #endregion

        #region AutoKill
        public static void AutoKill(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var plyList = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[0]);
                if (plyList.Count > 1)
                {
                    args.Player.SendErrorMessage("Player does not exist.");
                }
                else if (plyList.Count < 1)
                {
                    args.Player.SendErrorMessage(plyList.Count.ToString() + " players matched.");
                }
                else
                {
                    if (!plyList[0].Group.HasPermission("autokill") || args.Player == plyList[0])
                    {
                        var player = Utils.GetPlayers(plyList[0].Index);

                        player.autoKill = !player.autoKill;
                        if (player.autoKill)
                        {
                            args.Player.SendInfoMessage(plyList[0].Name + " is now being auto-killed.");
                            plyList[0].SendInfoMessage("You are now being auto-killed. " + 
                                " Beg for mercy, that you may be spared.");
                        }
                        else
                        {
                            args.Player.SendInfoMessage(plyList[0].Name + " is no longer being auto-killed.");
                            plyList[0].SendInfoMessage("You have been pardoned.");
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("You cannot autokill someone with the autokill permission.");
                    }
                }
            }
            else
            {
                args.Player.SendErrorMessage("Invalid syntax.  Proper Syntax: /autokill playername");
            }
        }
        #endregion

        #region TeamUnlock
        public static void TeamUnlock(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                string str = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));

                var player = Utils.GetPlayers(args.Player.Index);

                switch (args.Parameters[0].ToLower())
                {
                    case "red": if (str == MAC.config.redPass)
                        {
                            player.accessRed = true;
                            args.Player.SendErrorMessage("Red team unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "blue": if (str == MAC.config.bluePass)
                        {
                            player.accessBlue = true;
                            args.Player.SendMessage("Blue team unlocked.", Color.LightBlue);
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "green": if (str == MAC.config.greenPass)
                        {
                            player.accessGreen = true;
                            args.Player.SendSuccessMessage("Green team unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    case "yellow": 
                        if (str == MAC.config.yellowPass)
                        {
                            player.accessYellow = true;
                            args.Player.SendInfoMessage("Yellow unlocked.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Incorrect password.");
                        }
                        break;

                    default:
                        args.Player.SendErrorMessage("Invalid team color.");
                        break;
                }
            }
            else
            {
                args.Player.SendMessage("Improper Syntax.  Proper Syntax: /teamunlock teamcolor password", Color.Red);
            }
        }
        #endregion

        #region ViewAll
        public static void ViewAll(CommandArgs args)
        {
            var player = Utils.GetPlayers(args.Player.Index);

            player.viewAll = !player.viewAll;

            if (player.viewAll)
                args.Player.SendInfoMessage("View All mode has been turned on.");

            else
            {
                args.Player.SetTeam(Main.player[args.Player.Index].team);
                foreach (TSPlayer tply in TShock.Players)
                {
                    try
                    {
                        NetMessage.SendData((int)PacketTypes.PlayerTeam, args.Player.Index, -1, "", tply.Index);
                    }
                    catch (Exception) { }
                }
                args.Player.SendInfoMessage("View All mode has been turned off.");
            }
        }
        #endregion

        #region SpawnMobPlayer
        public static void SpawnMobPlayer(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 3)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount] [username]", Color.Red);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing mob name/id", Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 3 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount] [username]", Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShockAPI.TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
            var players = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[2]);
            if (players.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", Color.Red);
            }
            else if (players.Count > 1)
            {
                args.Player.SendMessage("More than one player matched!", Color.Red);
            }
            else if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, players[0].TileX, players[0].TileY, 50, 20);
                    TSPlayer.All.SendInfoMessage(string.Format("{0} was spawned {1} time(s) nearby {2}.", npc.name, amount, players[0].Name));
                }
                else
                    args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
        }
        #endregion

        #region FreezeTime
        public static void FreezeTime(CommandArgs args)
        {
            MAC.timeFrozen = !MAC.timeFrozen;
            MAC.freezeDayTime = Main.dayTime;
            MAC.timeToFreezeAt = Main.time;

            if (MAC.timeFrozen)
            {
                TSPlayer.All.SendInfoMessage(args.Player.Name.ToString() + " froze time.");
            }
            else
            {
                TSPlayer.All.SendInfoMessage(args.Player.Name.ToString() + " unfroze time.");
            }
        }
        #endregion

        #region KillAll
        public static void KillAll(CommandArgs args)
        {
            foreach (TSPlayer plr in TShock.Players)
            {
                if (plr != null)
                {
                    if (plr != args.Player)
                    {
                        plr.DamagePlayer(999999);
                    }
                }
            }
            TSPlayer.All.SendInfoMessage(args.Player.Name + " killed everyone!");
            args.Player.SendSuccessMessage("You killed everyone!");
        }
        #endregion

        #region ForceGive
        public static void ForceGive(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /forcegive <item type/id> <player> [item amount]", Color.Red);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing item name/id", Color.Red);
                return;
            }
            if (args.Parameters[1].Length == 0)
            {
                args.Player.SendMessage("Missing player name", Color.Red);
                return;
            }
            int itemAmount = 0;
            var items = TShockAPI.TShock.Utils.GetItemByIdOrName(args.Parameters[0]);
            args.Parameters.RemoveAt(0);
            string plStr = args.Parameters[0];
            args.Parameters.RemoveAt(0);
            if (args.Parameters.Count > 0)
                int.TryParse(args.Parameters[args.Parameters.Count - 1], out itemAmount);


            if (items.Count == 0)
            {
                args.Player.SendMessage("Invalid item type!", Color.Red);
            }
            else if (items.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
            }
            else
            {
                var item = items[0];
                if (item.type >= 1 && item.type < Main.maxItemTypes)
                {
                    var players = TShockAPI.TShock.Utils.FindPlayer(plStr);
                    if (players.Count == 0)
                    {
                        args.Player.SendMessage("Invalid player!", Color.Red);
                    }
                    else if (players.Count > 1)
                    {
                        args.Player.SendMessage("More than one player matched!", Color.Red);
                    }
                    else
                    {
                        var plr = players[0];
                        int stacks = 1;
                        if (itemAmount == 0)
                            itemAmount = item.maxStack;
                        if (itemAmount > item.maxStack)
                            stacks = itemAmount / item.maxStack + 1;
                        for (int i = 1; i < stacks; i++)
                            plr.GiveItem(item.type, item.name, item.width, item.height, item.maxStack);
                        if (itemAmount - (itemAmount / item.maxStack) * item.maxStack != 0)
                            plr.GiveItem(item.type, item.name, item.width, item.height, itemAmount - (itemAmount / item.maxStack) * item.maxStack);
                        args.Player.SendInfoMessage(string.Format("Gave {0} {1} {2}(s).", plr.Name, itemAmount, item.name));
                        plr.SendInfoMessage(string.Format("{0} gave you {1} {2}(s).", args.Player.Name, itemAmount, item.name));
                    }
                }
                else
                {
                    args.Player.SendMessage("Invalid item type!", Color.Red);
                }
            }
        }
        #endregion

        #region AutoHeal
        public static void AutoHeal(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                var player = Utils.GetPlayers(args.Player.Index);
                player.isHeal = !player.isHeal;

                if (player.isHeal)
                {
                    args.Player.SendSuccessMessage("Auto Heal Mode is now on.");
                }
                else
                {
                    args.Player.SendSuccessMessage("Auto Heal Mode is now off.");
                }
            }
            else
            {
                string str = args.Parameters[0];

                var findPlayers = TShockAPI.TShock.Utils.FindPlayer(str);

                if (findPlayers.Count > 1)
                {
                    args.Player.SendMessage("Player does not exist.", Color.Red);
                }

                else if (findPlayers.Count < 1)
                {
                    args.Player.SendMessage(findPlayers.Count + " players matched.", Color.Red);
                }

                else
                {
                    var player = Utils.GetPlayers(args.Parameters[0]);
                    TShockAPI.TSPlayer ply = findPlayers[0];

                    player.isHeal = !player.isHeal;

                    if (player.isHeal)
                    {
                        args.Player.SendInfoMessage("You have activated auto-heal for " + ply.Name + ".");
                        ply.SendInfoMessage(args.Player.Name + " has activated auto-heal on you");
                    }

                    else
                    {
                        args.Player.SendInfoMessage("You have deactivated auto-heal for " + ply.Name + ".");
                        ply.SendInfoMessage(args.Player.Name + " has deactivated auto-heal on you");
                    }
                }
            }
        }
        #endregion

        #region Ghost
        public static void Ghost(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                int tempTeam = args.Player.TPlayer.team;
                args.Player.TPlayer.team = 0;
                NetMessage.SendData(45, -1, -1, "", args.Player.Index);
                args.Player.TPlayer.team = tempTeam;

                var player = Utils.GetPlayers(args.Player.Index);

                if (!player.isGhost)
                {
                    args.Player.SendSuccessMessage("Ghost Mode activated!");
                    TSPlayer.All.SendInfoMessage(args.Player.Name + " left", Color.Yellow);
                }

                else
                {
                    args.Player.SendSuccessMessage("Ghost Mode deactivated!");
                    TSPlayer.All.SendInfoMessage(args.Player.Name + " has joined.", Color.Yellow);
                }

                player.isGhost = !player.isGhost;
                args.Player.TPlayer.position.X = 0;
                args.Player.TPlayer.position.Y = 0;
                MAC.cansend = true;
                NetMessage.SendData(13, -1, -1, "", args.Player.Index);
                MAC.cansend = false;
            }
            else
            {
                string str = "";
                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (i != args.Parameters.Count - 1)
                    {
                        str += args.Parameters[i] + " ";
                    }
                    else
                    {
                        str += args.Parameters[i];
                    }
                }

                var playerList = TShockAPI.TShock.Utils.FindPlayer(str);

                if (playerList.Count > 1)
                {
                    args.Player.SendErrorMessage("Player does not exist.");
                }

                else if (playerList.Count < 1)
                {
                    args.Player.SendErrorMessage(playerList.Count.ToString() + " players matched.");
                }

                else
                {
                    TShockAPI.TSPlayer Player = playerList[0];
                    int tempTeam = Player.TPlayer.team;
                    Player.TPlayer.team = 0;
                    NetMessage.SendData(45, -1, -1, "", Player.Index);
                    Player.TPlayer.team = tempTeam;
                    var Mplayer = Utils.GetPlayers(Player.Index);

                    if (!Mplayer.isGhost)
                    {
                        args.Player.SendSuccessMessage("Ghost mode activated for " + Player.Name + ".");
                        Player.SendInfoMessage(Player.Name + " has activated ghost mode for you");
                    }
                    else
                    {
                        args.Player.SendSuccessMessage("Ghost Mode deactivated for " + Player.Name + ".");
                        Player.SendInfoMessage(Player.Name + " has deactivated ghost mode for you");
                    }

                    Mplayer.isGhost = !Mplayer.isGhost;
                    Player.TPlayer.position.X = 0;
                    Player.TPlayer.position.Y = 0;
                    MAC.cansend = true;
                    NetMessage.SendData(13, -1, -1, "", Player.Index);
                    MAC.cansend = false;
                }
            }
        }
        #endregion

        //Repair Spawn Group

        #region MuteAll
        public static void MuteAll(CommandArgs args)
        {

            MAC.muteAll = !MAC.muteAll;
            if (MAC.muteAll)
            {
                MAC.config.muteAllReason = "";
                for (int i = 0; i < args.Parameters.Count; i++)
                {

                    MAC.config.muteAllReason += args.Parameters[i];
                    if (i < args.Parameters.Count - 1)
                    {

                        MAC.config.muteAllReason += " ";

                    }

                }
                if (MAC.config.muteAllReason == "")
                {

                    MAC.config.muteAllReason = MAC.config.defaultMuteAllReason;

                }
                TSPlayer.All.SendInfoMessage(args.Player.Name + " has muted everyone.");
                args.Player.SendSuccessMessage("You have muted everyone without the mute permission. " + 
                    "They will remain muted until you use /muteall again.");
            }
            else
            {
                foreach (Mplayer player in MAC.Players)
                {
                    player.muted = false;
                }
                TSPlayer.All.SendInfoMessage(args.Player.Name + " has unmuted everyone, except perhaps those muted before everyone was muted.");
            }
        }
        #endregion

        #region SpawnByMe
        public static void SpawnByMe(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnbyme <mob name/id> [amount]", Color.Red);
                return;
            }

            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing mob name/id", Color.Red);
                return;
            }

            int amount = 1;
            if (args.Parameters.Count >= 2 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnbyme <mob name/id> [amount]", Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = TShockAPI.TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);

            if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", Color.Red);
            }

            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
            }

            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, args.Player.TileX, args.Player.TileY, 2, 3);
                    TSPlayer.All.SendInfoMessage(string.Format("{0} was spawned {1} time(s).", npc.name, amount));
                }
                else
                    args.Player.SendMessage("Invalid mob type!", Color.Red);
            }
        }
        #endregion

        #region ButcherNear
        public static void ButcherNear(CommandArgs args)
        {

            int nearby = 50;
            if (args.Parameters.Count > 0)
            {
                try
                {
                    nearby = Convert.ToInt32(args.Parameters[0]);
                }
                catch { args.Player.SendErrorMessage("Improper Syntax. Proper Syntax: /butchernear [distance]"); return; }
            }
            int killcount = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if ((Main.npc[i].active) && (MAC.distance(new Vector2(Main.item[i].position.X, Main.item[i].position.Y), new Point((int)Main.player[args.Player.Index].position.X, (int)Main.player[args.Player.Index].position.Y)) < nearby * 16))
                {

                    TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                    killcount++;
                }
            }
            TSPlayer.All.SendInfoMessage(string.Format("Killed {0} NPCs within a radius of " + nearby.ToString() + " blocks.", killcount));

        }
        #endregion

        #region ButcherAll
        public static void ButcherAll(CommandArgs args)
        {
            int killcount = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                {
                    TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                    killcount++;
                }
            }
            TSPlayer.All.SendInfoMessage(string.Format("Killed {0} NPCs.", killcount));
        }
        #endregion

        #region ButcherFriendly
        public static void ButcherFriendly(CommandArgs args)
        {
            int killcount = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && Main.npc[i].townNPC)
                {
                    TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                    killcount++;
                }
            }
            TSPlayer.All.SendInfoMessage(string.Format("Killed {0} friendly NPCs.", killcount));
        }
        #endregion

        #region ButcherNPC
        public static void ButcherNPC(CommandArgs args)
        {

            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /butchernpc <npc name/id>", Color.Red);
                return;
            }

            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing npc name/id", Color.Red);
                return;
            }

            var npcs = TShockAPI.TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);

            if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid npc type!", Color.Red);
            }

            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) npc matched!", npcs.Count), Color.Red);
            }

            else
            {
                var npc = npcs[0];

                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    int killcount = 0;

                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == npc.type)
                        {
                            TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
                            killcount++;
                        }
                    }

                    TSPlayer.All.SendInfoMessage(string.Format("Killed {0} " + npc.name + "(s).", killcount));
                }

                else
                    args.Player.SendMessage("Invalid npc type!", Color.Red);
            }
        }
        #endregion

        #region Permamute
        public static void PermaMute(CommandArgs args)
        {
            if (args.Parameters.Count() > 0)
            {
                var tply = TShockAPI.TShock.Utils.FindPlayer(args.Parameters[0]);
                var readTableName = MAC.SQLEditor.ReadColumn("muteList", "Name", new List<SqlValue>());
                var readTableIP = MAC.SQLEditor.ReadColumn("muteList", "IP", new List<SqlValue>());

                if (tply.Count() > 1)
                {
                    args.Player.SendMessage("More than 1 player matched.", Color.Red);
                }

                else if (tply.Count() < 1)
                {
                    if (readTableName.Contains(args.Parameters[0].ToLower()))
                    {
                        List<SqlValue> List = new List<SqlValue>();
                        List<SqlValue> where = new List<SqlValue>();
                        where.Add(new SqlValue("Name", "'" + args.Parameters[0].ToLower() + "'"));
                        MAC.SQLWriter.DeleteRow("muteList", where);
                        args.Player.SendInfoMessage(args.Parameters[0] + 
                            " has been successfully been removed from the perma-mute list.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("No players found under that name on the server or in the perma-mute list.");
                    }
                }

                else
                {
                    var player = Utils.GetPlayers(tply[0].Index);
                    player.muteTime = -1;
                    string str = tply[0].Name.ToLower();
                    int index = MAC.SearchTable(MAC.SQLEditor.ReadColumn("muteList", "Name", new List<SqlValue>()), str);

                    if (index == -1)
                    {
                        List<SqlValue> theList = new List<SqlValue>();
                        theList.Add(new SqlValue("Name", "'" + str + "'"));
                        theList.Add(new SqlValue("IP", "'" + tply[0].IP + "'"));
                        MAC.SQLEditor.InsertValues("muteList", theList);
                        player.muted = true;
                        args.Player.SendInfoMessage(tply[0].Name + " has been permamuted by his/her IP Address.");
                        tply[0].SendErrorMessage("You have been muted by an admin.");
                    }

                    else
                    {
                        List<SqlValue> where = new List<SqlValue>();
                        where.Add(new SqlValue("IP", "'" + tply[0].IP + "'"));
                        MAC.SQLWriter.DeleteRow("muteList", where);
                        player.muted = false;
                        args.Player.SendInfoMessage(tply[0].Name + " has been taken off the perma-mute list, and is now un-muted.");
                        tply[0].SendInfoMessage("You have been unmuted.");
                    }
                }
            }

            else
            {
                args.Player.SendErrorMessage("Improper Syntax.  Proper Syntax: /permamute player");
            }
        }
        #endregion

        #region Reload
        public static void ReloadMore(CommandArgs args)
        {
            Utils.SetUpConfig();
            args.Player.SendInfoMessage("Reloaded MoreAdminCommands config file");
        }
        #endregion
    }
}
