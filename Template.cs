using System;
using System.IO;
using ClassicalSharp;

namespace PluginTemplate {
	public sealed class TestPlugin : Plugin {
		Game game;
		
		// Which version of the ClassicalSharp API this plugin is compatible with.
		// Note that only plugins with the exact same API version as the client get loaded.
		public int APIVersion { get { return 2; } }
		// Called at game end (destroy native resources)
		public void Dispose() { }
		
		// Called at game load
		public void Init(Game game) {
			this.game = game;
		}

		// Called at game load (after texture pack has been loaded)
		public void Ready(Game game) { }
		// Called when game has been reset (Player is reconnecting)
		public void Reset(Game game) { }
		// Called when player begins loading a new map
		public void OnNewMap(Game game) { }
		// Called after player finishes loading a new map
		public void OnNewMapLoaded(Game game) { }
	}
}
