using System;
using ClassicalSharp;
using ClassicalSharp.GraphicsAPI;

namespace AO {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.4"; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.MapRenderer.SetMeshBuilder(new AOMeshBuilder());
		}
		
		public void Ready(Game game) {
			game.MapRenderer.SetMeshBuilder(new AOMeshBuilder());
		}
		
		public void Reset(Game game) {
			game.MapRenderer.SetMeshBuilder(new AOMeshBuilder());
		}
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
}
