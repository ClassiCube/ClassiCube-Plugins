using System;
using System.IO;
using ClassicalSharp;

namespace PingPlugin {
	public sealed class Pinger : Plugin {
		public int APIVersion { get { return 2; } }
		public void Dispose() { }
		
		string[] words, endings;
		public void Init(Game game) {
			Events.ChatReceived += CheckPing;
			if (!File.Exists("ping_words.txt")) {
				File.WriteAllLines("ping_words.txt", new string[] { "unk", "unknown" });
			}
			
			// Message counts as a ping when it: contains ' word ' or ends with ' word'
			string[] pings = File.ReadAllLines("ping_words.txt");
			for (int i = 0; i < pings.Length; i++) {
				pings[i] = pings[i].Trim().ToLower();
			}
			
			words   = new string[pings.Length];
			endings = new string[pings.Length];
			for (int i = 0; i < pings.Length; i++) {
				words[i]   = " " + pings[i] + " ";
				endings[i] =       pings[i] + " ";
			}
		}

		void CheckPing(ref string raw, MessageType type) {
			if (String.IsNullOrEmpty(raw))  return;
			if (type != MessageType.Normal) return;
			string msg = Utils.StripColours(raw).ToLower();
			
			for (int i = 0; i < words.Length; i++) {
				bool has  = msg.IndexOf(words[i]) >= 0;
				bool ends = msg.EndsWith(endings[i]);
				if (!has && !ends) continue;
				
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
