using System;
using ClassicalSharp;
using ClassicalSharp.Commands;
using ClassicalSharp.Network;

namespace SetName {

	public sealed class SetNamePlugin : Plugin {
		public int APIVersion { get { return 2; } }
		public void Dispose() { }
		
		public void Init(Game game) {
			game.CommandList.Register(new SetNameCommand());
		}
		
		public void Ready(Game game) { }
		public void Reset(Game game) { }
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
	}

	public sealed class SetNameCommand : Command {
		public SetNameCommand() {
			Name = "SetName";
			Help = new string[] {
				"&a/client setname [name]",
				"&eSets the name of the software shown in /clients.",
				"&eCan have spaces and colour codes.",
			};
		}
		
		static void WriteHackyString(NetWriter w, string value) {
			int count = Math.Min(value.Length, Utils.StringLength);

			for (int i = 0; i < count; i++) {
				w.WriteUInt8(Utils.UnicodeToCP437(value[i]));
			}			
			for (int i = value.Length; i < Utils.StringLength; i++) {
				w.WriteUInt8((byte)' ');
			}
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) {
				game.Chat.Add("&cNew software name required."); return;
			}
			
			string name = "";
			for (int i = 1; i < args.Length; i++) {
				name += args[i] + " ";
			}
			
			name = name.Trim();
			game.Server.AppName = name;
			
			if (!game.Server.IsSinglePlayer) {
				NetworkProcessor net = (NetworkProcessor)game.Server;
				net.writer.WriteUInt8(Opcode.CpeExtInfo);
				WriteHackyString(net.writer, name);
				net.writer.WriteInt16(0); // no extensions
				net.SendPacket();
			}
			game.Chat.Add("&eSet software name to: " + name);
		}
	}
}