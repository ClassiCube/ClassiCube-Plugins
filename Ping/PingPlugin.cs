using System;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Events;

namespace InfDevImportPlugin {
	public sealed class Core : Plugin {
		public string ClientVersion { get { return "0.99.9.96"; } }
		public void Dispose() { }
		
		string[] pings;
		public void Init(Game game) {
			game.Events.ChatReceived += CheckPing;
			if (!File.Exists("ping_words.txt")) {
				File.WriteAllLines("ping_words.txt", new string[] { "unk", "unknown" });
			}
			
			pings = File.ReadAllLines("ping_words.txt");
			for (int i = 0; i < pings.Length; i++) {
				pings[i] = " " + pings[i].Trim().ToLower() + " ";
			}
		}

		void CheckPing(object sender, ChatEventArgs e) {
			if (String.IsNullOrEmpty(e.Text)) return;
			string message = Utils.StripColours(e.Text).ToLower();
			
			foreach (string ping in pings) {
				if (message.IndexOf(ping) == -1) continue;
				
				Console.Beep(); 
				Console.Beep(); 
				Console.Beep();
				return;
			}
		}
		
		public void Ready(Game game) { }
		public void Reset(Game game) { }
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
	}
}
