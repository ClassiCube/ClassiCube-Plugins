using System;
using ClassicalSharp;
using ClassicalSharp.GraphicsAPI;

namespace OffsetCameraPlugin {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.4"; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.Cameras.Add(new GoodlyayCamera(game));
		}
		
		public void Ready(Game game) { }
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
}
