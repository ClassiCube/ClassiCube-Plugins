using System;
using ClassicalSharp;

namespace PluginObjExport {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.9.1"; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.CommandList.Register(new ObjExporterCommand());
		}
		
		public void Ready(Game game) { }
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}
}
