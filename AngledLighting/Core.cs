using System;
using ClassicalSharp;

namespace AngledLightingPlugin {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.4"; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.Lighting.Dispose();
			game.Components.Remove(game.Lighting);
			
			game.Lighting = new AngledLighting();
			game.Lighting.Init(game);
			game.Components.Add(game.Lighting);
		}
		
		public void Ready(Game game) { }
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
}
