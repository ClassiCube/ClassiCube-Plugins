using System;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Commands;
using ClassicalSharp.Generator;
using ClassicalSharp.Map;
using ClassicalSharp.Textures;
using OpenTK;

namespace PluginObjExport {

	public sealed class ObjExporter : Plugin {
		public int APIVersion { get { return 200; } }
		public void Dispose() { }
		
		public void Init(Game game) {
			game.CommandList.Register(new ObjExporterCommand());
		}
		
		public void Ready(Game game) { }
		public void Reset(Game game) { }
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
	}

	public sealed class ObjExporterCommand : Command {
		public ObjExporterCommand() {
			Name = "ObjExport";
			Help = new string[] {
				"&a/client objexport [filename] ['all' for all faces] ['no' for not mirror]",
				"&eExports the current map to the OBJ file format, as [filename].obj.",
				"&e  (excluding faces of blocks named 'Invalid')",
				"&eThis can then be imported into 3D modelling software such as Blender."
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) {
				game.Chat.Add("&cFilename required."); return;
			}
			
			string path = Path.Combine("maps", args[1]);
			path = Path.ChangeExtension(path, ".obj");
			
			using (FileStream fs = File.Create(path)) {
				ObjExporter obj = new ObjExporter();
				
				obj.all = args.Length > 2 && Utils.CaselessEq(args[2], "ALL");
				if (obj.all) {
					game.Chat.Add("&cExporting all faces - slow!");
				}
				
				obj.mirror = args.Length <= 3 || !Utils.CaselessEq(args[3], "NO");
				if (!obj.mirror) {
					game.Chat.Add("&cNot mirroring sprites!");
				}
				
				obj.Save(fs, game);
			}
			game.Chat.Add("&eExported map to " + path);
		}
	}

	public sealed class ObjExporter : IMapFormatExporter {
		
		World map;
		int maxX, maxY, maxZ;
		int oneX, oneY, oneZ;
		byte[] blocks, blocks2;
		StreamWriter w;
		int[] texI = new int[768];
		public bool all = false, mirror = true;
		bool[] include = new bool[768];
		
		public void Save(Stream stream, Game game) {
			SetVars(game);
			using (StreamWriter w = new StreamWriter(stream)) {
				this.w = w;
				for (ushort b = 0; b <= BlockInfo.MaxUsed; b++) {
					include[b] = BlockInfo.Draw[b] != DrawType.Gas && BlockInfo.Name[b] != "Invalid";
				}
				
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
			blocks2 = map.blocks2;
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
			int x, y;
			float u = 1.0f / 16, v = 1.0f / Atlas2D.RowsCount;
			
			for (ushort b = 0; b <= BlockInfo.MaxUsed; b++) {
				if (!include[b]) continue;
				
				Vector3 min = BlockInfo.MinBB[b], max = BlockInfo.MaxBB[b];
				w.WriteLine("#" + BlockInfo.Name[b]);
				if (BlockInfo.Draw[b] == DrawType.Sprite) {
					min = Vector3.Zero;
					max = Vector3.One;
				}
				texI[b] = i;
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Left), out x, out y);
				w.WriteLine("vt " + ((x + min.Z) * u) + " " + ((y + min.Y) * v));
				w.WriteLine("vt " + ((x + min.Z) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + max.Z) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + max.Z) * u) + " " + ((y + min.Y) * v));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Right), out x, out y);
				w.WriteLine("vt " + ((x + max.Z) * u) + " " + ((y + min.Y) * v));
				w.WriteLine("vt " + ((x + max.Z) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + min.Z) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + min.Z) * u) + " " + ((y + min.Y) * v));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Front), out x, out y);
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + min.Y) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + min.Y) * v));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Back), out x, out y);
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + min.Y) * v));
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + max.Y) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + min.Y) * v));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Bottom), out x, out y);
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + max.Z) * v));
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + min.Z) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + min.Z) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + max.Z) * v));
				
				Unpack(BlockInfo.GetTextureLoc(b, Side.Top), out x, out y);
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + max.Z) * v));
				w.WriteLine("vt " + ((x + min.X) * u) + " " + ((y + min.Z) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + min.Z) * v));
				w.WriteLine("vt " + ((x + max.X) * u) + " " + ((y + max.Z) * v));
				i += 4 * 6;
			}
		}
		
		static void Unpack(int texLoc, out int x, out int y) {
			x = (texLoc % Atlas2D.TilesPerRow);
			y = (Atlas2D.RowsCount - 1) - (texLoc / Atlas2D.TilesPerRow);
		}
		
		static bool IsFaceHidden(int block, int other, int side) {
			return (BlockInfo.hidden[(block * BlockInfo.Count) + other] & (1 << side)) != 0;
		}
		
		static JavaRandom spriteRng = new JavaRandom(0);
		void DumpVertices() {
			w.WriteLine("#vertices");
			int i = -1, mask = BlockInfo.IDMask;
			Vector3 min, max;
			
			for (int y = 0; y < map.Height; y++)
				for (int z = 0; z < map.Length; z++)
					for (int x = 0; x < map.Width; x++)
			{
				++i; int b = (blocks[i] | (blocks2[i] << 8)) & mask;
				if (!include[b]) continue;
				min.X = x; min.Y = y; min.Z = z;
				max.X = x; max.Y = y; max.Z = z;
				
				if (BlockInfo.Draw[b] == DrawType.Sprite) {
					min.X += 2.50f/16f; min.Z += 2.50f/16f;
					max.X += 13.5f/16f; max.Z += 13.5f/16f; max.Y += 1.0f;
					
					byte offsetType = BlockInfo.SpriteOffset[b];
					if (offsetType >= 6 && offsetType <= 7) {
						spriteRng.SetSeed((x + 1217 * z) & 0x7fffffff);
						float valX = spriteRng.Next(-3, 3 + 1) / 16.0f;
						float valY = spriteRng.Next(0,  3 + 1) / 16.0f;
						float valZ = spriteRng.Next(-3, 3 + 1) / 16.0f;
						
						const float stretch = 1.7f / 16.0f;
						min.X += valX - stretch; max.X += valX + stretch;
						min.Z += valZ - stretch; max.Z += valZ + stretch;
						if (offsetType == 7) { min.Y -= valY; max.Y -= valY; }
					}

					// Draw Z axis
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
					
					// Draw Z axis mirrored
					if (mirror) {
						w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
						w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
						w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
						w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					}

					// Draw X axis
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
					
					// Draw X axis mirrored
					if (mirror) {
						w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
						w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
						w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
						w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					}
					continue;
				}
				
				min += BlockInfo.RenderMinBB[b];
				max += BlockInfo.RenderMaxBB[b];
				
				// minx
				if (x == 0 || all || !IsFaceHidden(b, (blocks[i - oneX] | (blocks2[i - oneX] << 8)) & mask, Side.Left)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
				}
				
				// maxx
				if (x == maxX || all || !IsFaceHidden(b, (blocks[i + oneX] | (blocks2[i + oneX] << 8)) & mask, Side.Right)) {
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
				}
				
				// minz
				if (z == 0 || all || !IsFaceHidden(b, (blocks[i - oneZ] | (blocks2[i - oneZ] << 8)) & mask, Side.Front)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
				}
				
				// maxz
				if (z == maxZ || all || !IsFaceHidden(b, (blocks[i + oneZ] | (blocks2[i + oneZ] << 8)) & mask, Side.Back)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
				}
				
				// miny
				if (y == 0 || all || !IsFaceHidden(b, (blocks[i - oneY] | (blocks2[i - oneY] << 8)) & mask, Side.Bottom)) {
					w.WriteLine("v " + min.X + " " + min.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + min.Y + " " + min.Z);
				}
				
				// maxy
				if (y == maxY || all || !IsFaceHidden(b, (blocks[i + oneY] | (blocks2[i + oneY] << 8)) & mask, Side.Top)) {
					w.WriteLine("v " + min.X + " " + max.Y + " " + min.Z);
					w.WriteLine("v " + min.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + max.Z);
					w.WriteLine("v " + max.X + " " + max.Y + " " + min.Z);
				}
			}
		}
		
		void DumpFaces() {
			w.WriteLine("#faces");
			int i = -1, j = 1, mask = BlockInfo.IDMask;
			
			for (int y = 0; y < map.Height; y++)
				for (int z = 0; z < map.Length; z++)
					for (int x = 0; x < map.Width; x++)
			{
				++i; int b = (blocks[i] | (blocks2[i] << 8)) & mask;
				if (!include[b]) continue;
				int k = texI[b], n = 1;
				
				if (BlockInfo.Draw[b] == DrawType.Sprite) {
					n += 6;
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					if (mirror) {
						w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					}
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					if (mirror) {
						w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4; n++;
					}
					continue;
				}

				// minx
				if (x == 0 || all || !IsFaceHidden(b, (blocks[i - oneX] | (blocks2[i - oneX] << 8)) & mask, Side.Left)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxx
				if (x == maxX || all || !IsFaceHidden(b, (blocks[i + oneX] | (blocks2[i + oneX] << 8)) & mask, Side.Right)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
				
				// minz
				if (z == 0 || all || !IsFaceHidden(b, (blocks[i - oneZ] | (blocks2[i - oneZ] << 8)) & mask, Side.Front)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxz
				if (z == maxZ || all || !IsFaceHidden(b, (blocks[i + oneZ] | (blocks2[i + oneZ] << 8)) & mask, Side.Back)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// miny
				if (y == 0 || all || !IsFaceHidden(b, (blocks[i - oneY] | (blocks2[i - oneY] << 8)) & mask, Side.Bottom)) {
					w.WriteLine("f " + (j+3)+"/"+(k+3)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+0)+"/"+(k+0)+"/"+n); j += 4;
				} k += 4; n++;
				
				// maxy
				if (y == maxY || all || !IsFaceHidden(b, (blocks[i + oneY] | (blocks2[i + oneY] << 8)) & mask, Side.Top)) {
					w.WriteLine("f " + (j+0)+"/"+(k+0)+"/"+n + " " + (j+1)+"/"+(k+1)+"/"+n + " " + (j+2)+"/"+(k+2)+"/"+n + " " + (j+3)+"/"+(k+3)+"/"+n); j += 4;
				} k += 4; n++;
			}
		}
	}
}