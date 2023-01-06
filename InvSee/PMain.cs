using System;
using System.Reflection;
using InvSee.Extensions;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace InvSee
{
	[ApiVersion(2, 1)]
	public class PMain : TerrariaPlugin
	{
		public override string Author => "Enerdy制作,nnt汉化,Cai升级";

		public override string Description => "临时复制用户背包,并对其进行修改.";

		public override string Name => "InvSee汉化版";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public static string Tag => TShock.Utils.ColorTag("InvSee:", Color.Teal);

		public PMain(Main game)
			: base(game)
		{
			base.Order--;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				PlayerHooks.PlayerLogout -= OnLogout;
			}
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerLogout += OnLogout;
		}

		private void OnInitialize(EventArgs e)
		{
			Action<Command> action = delegate(Command command)
			{
				TShockAPI.Commands.ChatCommands.RemoveAll(delegate(Command c)
				{
					foreach (string name in c.Names)
					{
						if (command.Names.Contains(name))
						{
							return true;
						}
					}
					return false;
				});
				TShockAPI.Commands.ChatCommands.Add(command);
			};
			action(new Command(Permissions.InvSee, Commands.DoInvSee, "invsee", "查背包")
			{
				HelpDesc = new string[2] { "用目标玩家的背包替换你自己的背包.", $"输入\"{Commands._cp}查背包\"来恢复你之前的背包." }
			});
		}

		private void OnLeave(LeaveEventArgs e)
		{
			if (e.Who >= 0 && e.Who <= Main.maxNetPlayers)
			{
				TSPlayer tSPlayer = TShock.Players[e.Who];
				if (tSPlayer != null)
				{
					PlayerInfo playerInfo = tSPlayer.GetPlayerInfo();
					playerInfo.Restore(tSPlayer);
				}
			}
		}

		private void OnLogout(PlayerLogoutEventArgs e)
		{
			if (e.Player != null && e.Player.Active && e.Player.RealPlayer)
			{
				e.Player.GetPlayerInfo().Restore(e.Player);
			}
		}
	}
}
