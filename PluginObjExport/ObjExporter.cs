using System;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Commands;
using ClassicalSharp.Map;
using OpenTK;

namespace PluginObjExport {

	/// <summary> Command that displays information about the user's GPU. </summary>
	public sealed class ObjExporterCommand : Command {
		
		public ObjExporterCommand() {
			Name = "ObjExport";
			Help = new string[] {
				"&a/client objexport [filename]",
				"&eExports the current map to the OBJ file format, as [filename].obj.",
				"&eThis can then be imported into 3D modelling software such as Blender.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) { game.Chat.Add("&cFilename required."); return; }
			
			string file = Path.Combine("maps", args[1]);
			file = Path.ChangeExtension(file, ".obj");
			
			using (FileStream src = File.Create(file)) {
				new ObjExporter().Save(src, game);
			}
			game.Chat.Add("&eExported map to " + file);
		}
	}
	
	public sealed class ObjExporter : IMapFormatExporter {
		
		World map;
		int maxX, maxY, maxZ;
		int oneX, oneY, oneZ;
		byte[] blocks;
		StreamWriter w;
		int[] texI = new int[256];
		
		public void Save(Stream stream, Game game) {
			SetVars(game);
			using (StreamWriter w = new StreamWriter(stream)) {
				this.w = w;
				DumpNormals();
				DumpTextures();
				DumpVertices();
				DumpFaces();
			}
		}
		
		void SetVars(Game game) {
			map = game.World;
			maxX = map.Width - 1; maxY = map.Height - 1; maxZ = map.Length - 1;
			oneX = 1; oneY = map.Width * map.Length; oneZ = map.Width;
			blocks = map.blocks;
		}
		
		void DumpNormals() {
			w.WriteLine("#normals");
			w.WriteLine("vn -1.0 0.0 0.0");
			w.WriteLine("vn 1.0 0.0 0.0");
			w.WriteLine("vn 0.0 0.0 -1.0");
			w.WriteLine("vn 0.0 0.0 1.0");
			w.WriteLine("vn 0.0 -1.0 0.0");
			w.WriteLine("vn 0.0 1.0 0.0");
			w.WriteLine("#sprite normals");
			w.WriteLine("vn -0.70710678 0 0.70710678");			
			w.WriteLine("vn 0.70710678 0 -0.70710678");			
			w.WriteLine("vn 0.70710678 0 0.70710678");
			w.WriteLine("vn -0.70710678 0 -0.70710678");
		}
		
		void DumpTextures() {
			w.WriteLine("#textures");
			int i = 1;
			float x, y;
			
			for (byte b = 0; b < 255; b++) {
				if (BlockInfo.Name[b] == "Invalid") continue;
				
				Vector3 min = BlockInfo.MinBB[b] / 16.0f, max = BlockInfo.MaxBB[b] / 16.0f;
				w.WriteLine("#" + BlockInfo.Name[b]);
				if (BlockInfo.Draw[b] == DrawType.Sprite) {
					min = Vector3.Zero;
					max = Vector3.One / 16.0f;
				}
				texI[b] = i;
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Left), out x, out y);
				w.WriteLine("vt " + (x + min.Z) + " " + (y + min.Y));
				w.WriteLine("vt " + (x + min.Z) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + max.Z) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + max.Z) + " " + (y + min.Y));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Right), out x, out y);
				w.WriteLine("vt " + (x + max.Z) + " " + (y + min.Y));
				w.WriteLine("vt " + (x + max.Z) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + min.Z) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + min.Z) + " " + (y + min.Y));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Front), out x, out y);
				w.WriteLine("vt " + (x + max.X) + " " + (y + min.Y));
				w.WriteLine("vt " + (x + max.X) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + min.X) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + min.X) + " " + (y + min.Y));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Back), out x, out y);
				w.WriteLine("vt " + (x + min.X) + " " + (y + min.Y));
				w.WriteLine("vt " + (x + min.X) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + max.X) + " " + (y + max.Y));
				w.WriteLine("vt " + (x + max.X) + " " + (y + min.Y));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Bottom), out x, out y);
				w.WriteLine("vt " + (x + min.X) + " " + (y + max.Z));
				w.WriteLine("vt " + (x + min.X) + " " + (y + min.Z));
				w.WriteLine("vt " + (x + max.X) + " " + (y + min.Z));
				w.WriteLine("vt " + (x + max.X) + " " + (y + max.Z));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Top), out x, out y);
				w.WriteLine("vt " + (x + min.X) + " " + (y + max.Z));
				w.WriteLine("vt " + (x + min.X) + " " + (y + min.Z));
				w.WriteLine("vt " + (x + max.X) + " " + (y + min.Z));
				w.WriteLine("vt " + (x + max.X) + " " + (y + max.Z));
				i += 4 * 6;
			}
		}
		
		static void Unpack(int texId, out float x, out float y) {
			x = (texId & 0x0F) / 16.0f;
			y = 1 - (texId >> 4) / 16.0f - (1 / 16.0f);
		}
		
		void DumpVertices() {
			w.WriteLine("#vertices");
			int i = -1;
			Vector3 min, max;
			
			for (int y = 0; y < map.Height; y++)
				for (int z = 0; z < map.Length; z++)
					for (int x = 0; x < map.Width; x++)
			{
				++i; byte block = blocks[i];
				if (BlockInfo.Draw[block] == DrawType.Gas) continue;
				min.X = x; min.Y = y; min.Z = z;
				max.X = x; max.Y = y; max.Z = z;
				
				if (BlockInfo.Draw[block] == DrawType.Sprite) {
					min.X += 2.50f/16f; min.Z += 2.50f/16f;
					max.X += 13.5f/16f; max.Z += 13.5f/16f; max.Y += 1.0f;

					// Draw Z axis
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
					
					// Draw Z axis mirrored
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);

					// Draw X axis
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
					
					// Draw X axis mirrored
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					continue;
				}
				
				min += BlockInfo.RenderMinBB[block];
				max += BlockInfo.RenderMaxBB[block];
				
				// minx
				if (x == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneX], Side.Left)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
				}
				
				// maxx
				if (x == maxX || !BlockInfo.IsFaceHidden(block, blocks[i + oneX], Side.Right)) {
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
				}
				
				// minz
				if (z == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneZ], Side.Front)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
				}
				
				// maxz
				if (z == maxZ || !BlockInfo.IsFaceHidden(block, blocks[i + oneZ], Side.Back)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
				}
				
				// miny
				if (y == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneY], Side.Bottom)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
				}
				
				// maxy
				if (y == maxY || !BlockInfo.IsFaceHidden(block, blocks[i + oneY], Side.Top)) {
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
				}
			}
		}
		
		void DumpFaces() {
			w.WriteLine("#faces");
			int i = -1, j = 1;
			
			for (int y = 0; y < map.Height; y++)
				for (int z = 0; z < map.Length; z++)
					for (int x = 0; x < map.Width; x++)
			{
				++i; byte block = blocks[i];
				if (BlockInfo.Draw[block] == DrawType.Gas) continue;
				int k = texI[block], n = 1;
				
				if (BlockInfo.Draw[block] == DrawType.Sprite) {
					n += 6;
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					continue;
				}
				
				Vector3 min = new Vector3(x, y, z) + BlockInfo.RenderMinBB[block];
				Vector3 max = new Vector3(x, y, z) + BlockInfo.RenderMaxBB[block];
				
				// minx
				if (x == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneX], Side.Left)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxx
				if (x == maxX || !BlockInfo.IsFaceHidden(block, blocks[i + oneX], Side.Right)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
				
				// minz
				if (z == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneZ], Side.Front)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxz
				if (z == maxZ || !BlockInfo.IsFaceHidden(block, blocks[i + oneZ], Side.Back)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// miny
				if (y == 0 || !BlockInfo.IsFaceHidden(block, blocks[i - oneY], Side.Bottom)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxy
				if (y == maxY || !BlockInfo.IsFaceHidden(block, blocks[i + oneY], Side.Top)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
			}
		}
	}
}
