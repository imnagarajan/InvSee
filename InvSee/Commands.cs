using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InvSee.Extensions;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace InvSee
{
	internal class Commands
	{
		public static readonly string _cp = TShockAPI.Commands.Specifier;

		public static void DoInvSee(CommandArgs args)
		{
			if (!Main.ServerSideCharacter)
			{
				args.Player.PluginErrorMessage("服务器云存档(SSC)必须打开.");
				return;
			}
			if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("[InvSee]:你必须在游戏中使用.");
				return;
			}
			PlayerInfo playerInfo = args.Player.GetPlayerInfo();
			if (args.Parameters.Count < 1)
			{
				if (args.Player.Dead)
				{
					args.Player.PluginErrorMessage("不能在死亡后恢复你的背包.");
					return;
				}
				if (playerInfo.Restore(args.Player))
				{
					args.Player.PluginErrorMessage("背包已恢复.");
					return;
				}
				args.Player.PluginInfoMessage("你目前没有查看任何人的背包.");
				args.Player.SendInfoMessage($"输入[c/FF000:{_cp}查背包 帮助]查看更多信息.");
				return;
			}
			if (args.Parameters.Count == 0 || args.Parameters[0].ToLower() == "help")
			{
				args.Player.PluginErrorMessage("查背包插件,查询与修改用户背包.");
				args.Player.SendInfoMessage($"{_cp}查背包 <用户名> - 查看复制一个用户背包");
				if (args.Player.HasPermission(Permissions.InvSeeSave))
				{
					args.Player.SendInfoMessage($"输入[c/FF0000:{_cp}查背包]命令后,对目标用户的背包进行修改");
					args.Player.SendInfoMessage($"再输入[c/FF0000:{_cp}查背包 保存]可以将修改后的背包保存,并应用到目标用户");
				}
				return;
			}
			Regex regex = new Regex("^\\w+ (?:(?<Saving>保存(?:ave)?)|\"?(?<Name>.+?)\"?)$");
			Match match = regex.Match(args.Message);
			if (!string.IsNullOrWhiteSpace(match.Groups["Saving"].Value))
			{
				if (!args.Player.Group.HasPermission(Permissions.InvSeeSave))
				{
					args.Player.PluginErrorMessage("你没有权限来更改用户的背包!");
					return;
				}
				if (playerInfo.Backup == null || string.IsNullOrWhiteSpace(playerInfo.CopyingUserName))
				{
					args.Player.PluginErrorMessage("你必须打开一个用户背包后再使用该命令!");
					return;
				}
				UserAccount userAccountByName = TShock.UserAccounts.GetUserAccountByName(playerInfo.CopyingUserName);
				if (userAccountByName == null)
				{
					args.Player.PluginErrorMessage("错误的用户名!");
					return;
				}
				TSPlayer ?player;
				if ((player = TSPlayer.FindByNameOrID(playerInfo.CopyingUserName).FirstOrDefault()) != null)
				{
					args.Player.PlayerData.CopyCharacter(args.Player);
					args.Player.PlayerData.RestoreCharacter(player);
					TShock.Log.ConsoleInfo("[InvSee]:{0} 已修改 {1}[在线](用户ID{2})的背包.", args.Player.Name, playerInfo.CopyingUserName, playerInfo.UserID);
				}
				else
				{
					try
					{
						args.Player.PlayerData.CopyCharacter(args.Player);
						PlayerData playerData = args.Player.PlayerData;
						string query = "UPDATE tsCharacter SET Health = @0, MaxHealth = @1, Mana = @2, MaxMana = @3, Inventory = @4 WHERE Account = @5;";
						TShock.CharacterDB.database.Query(query, playerData.health, playerData.maxHealth, playerData.mana, playerData.maxMana, string.Join("~", playerData.inventory), playerInfo.UserID);
						TShock.Log.ConsoleInfo("[InvSee]:{0} 已修改 {1}[离线](用户ID{2})的背包.", args.Player.Name, playerInfo.CopyingUserName, playerInfo.UserID);
					}
					catch (Exception ex)
					{
						args.Player.PluginErrorMessage("修改用户背包时出现错误.");
						TShock.Log.Error(ex.ToString());
						return;
					}
				}
				args.Player.PluginInfoMessage("已保存修改 " + userAccountByName.Name + " 的背包.");
				return;
			}
			string value = match.Groups["Name"].Value;
			List<TSPlayer> list = TSPlayer.FindByNameOrID(value);
			PlayerData playerData2;
			string text;
			int iD;
			if (list.Count == 0)
			{
				if (!args.Player.Group.HasPermission(Permissions.InvSeeUser))
				{
					args.Player.PluginErrorMessage("你没有权限使用该命令!");
					return;
				}
				UserAccount userAccountByName2 = TShock.UserAccounts.GetUserAccountByName(value);
				if (userAccountByName2 == null)
				{
					args.Player.PluginErrorMessage("错误的用户名\"" + value + "\"!");
					return;
				}
				playerData2 = TShock.CharacterDB.GetPlayerData(args.Player, userAccountByName2.ID);
				text = userAccountByName2.Name;
				iD = userAccountByName2.ID;
			}
			else
			{
				if (list.Count > 1)
				{
					args.Player.SendMultipleMatchError(list.Select((TSPlayer p) => p.Name));
					return;
				}
				if (list[0].Account == null)
				{
					args.Player.PluginErrorMessage("错误的用户名\"" + value + "\"!");
					return;
				}
				iD = list[0].Account.ID;
				list[0].PlayerData.CopyCharacter(list[0]);
				playerData2 = list[0].PlayerData;
				UserAccount account = list[0].Account;
				text = ((account != null) ? account.Name : null) ?? "";
			}
			try
			{
				if (playerData2 == null)
				{
					args.Player.PluginErrorMessage(text + "的数据没有找到!");
					return;
				}
				if (playerInfo.Backup == null)
				{
					playerInfo.Backup = new PlayerData(args.Player);
					playerInfo.Backup.CopyCharacter(args.Player);
				}
				playerInfo.CopyingUserName = text;
				playerInfo.UserID = iD;
				playerData2.RestoreCharacter(args.Player);
				args.Player.PluginSuccessMessage("已打开 " + text + " 的背包.");
				if (args.Player.HasPermission(Permissions.InvSeeSave))
				{
					args.Player.SendInfoMessage($"在此期间你可以更改他的背包内容,并输入[c/FF0000:{_cp}查背包 保存]将其保存");
                    args.Player.SendInfoMessage($"输入\"{Commands._cp}查背包\"来恢复你之前的背包.");


				}
			}
			catch (Exception ex2)
			{
				if (playerInfo.Backup != null)
				{
					playerInfo.CopyingUserName = "";
					playerInfo.Backup.RestoreCharacter(args.Player);
					playerInfo.Backup = null;
				}
				TShock.Log.ConsoleError(ex2.ToString());
				args.Player.PluginErrorMessage("出现了一些错误,已恢复你的背包.");
			}
		}
	}
}
