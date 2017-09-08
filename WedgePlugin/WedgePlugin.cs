using System;
using ClassicalSharp;
using ClassicalSharp.GraphicsAPI;

namespace WedgePlugin {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.9.3"; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
		}
		
		public void Ready(Game game) { 
			game.MapRenderer.SetMeshBuilder(new WedgeMeshBuilder());
			if (game.World.blocks == null) return;
			game.MapRenderer.Refresh();
		}
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
}