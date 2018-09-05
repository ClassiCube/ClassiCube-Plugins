// https://minecraft.gamepedia.com/index.php?title=Chunk_format&oldid=26072
// https://minecraft.gamepedia.com/index.php?title=Java_Edition_data_values&oldid=6992
using System;
using System.IO;
using System.IO.Compression;
using ClassicalSharp;
using ClassicalSharp.Commands;
using ClassicalSharp.Map;
using ClassicalSharp.Network;
using NbtCompound = System.Collections.Generic.Dictionary<string, ClassicalSharp.Map.NbtTag>;

namespace InfDevImportPlugin {
	public sealed class InfdevConverter : Plugin {		
		public int APIVersion { get { return 1; } }		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.CommandList.Register(new InfdevConvertCommand());
		}
		
		public void Ready(Game game) { }	
		public void Reset(Game game) { }		
		public void OnNewMap(Game game) { }		
		public void OnNewMapLoaded(Game game) { }
	}
	
	public sealed class InfdevConvertCommand : Command {		
		public InfdevConvertCommand() {
			Name = "InfConvert";
			Help = new string[] {
				"&a/client infconvert [world number] [start x] [start z] [end x] [end z].",
				"&aConverts region of chunks from [start x, start z] to [end x, end z]",
                "&aTested on infev. Probably works for Alpha too.",
			};
		}
		
		static string Base36(int value) {
			string sign = "", encoded = "";
			const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
			if (value == 0) return "0";
			if (value < 0) { sign = "-"; value = -value; }
			
			while (value > 0) {
				int rem = value % 36; value /= 36;
				encoded = alphabet[rem] + encoded;
			}
			return sign + encoded;
		}
		
		public override void Execute(string[] args) {
			if (args.Length <= 5) {
				game.Chat.Add("&cNot enough args"); return;
			}
			
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string map = appData + "\\.minecraft\\saves\\World" + args[1];
			
			int startX, startZ, endX, endZ;
			if (!int.TryParse(args[2], out startX) || !int.TryParse(args[3], out startZ) ||
			    !int.TryParse(args[4], out endX)   || !int.TryParse(args[5], out endZ)) {
				game.Chat.Add("&cStartX/StartZ/EndX/EndZ weren't integers");
				return;
			}
			
			int width  = (endX - startX + 1) * 16;
			int length = (endZ - startZ + 1) * 16;
			byte[] blocks = new byte[width * 128 * length];
			
			for (int cz = startZ; cz <= endZ; cz++)
				for (int cx = startX; cx <= endX; cx++)
			{
				string folder1 = Base36(cx & 63), folder2 = Base36(cz & 63);
				string chunk1  = Base36(cx),      chunk2  = Base36(cz);
				string path = map + "\\" + folder1 + "\\" + folder2 + "\\c." + chunk1 + "." + chunk2 + ".dat";
				
				if (!File.Exists(path)) {
					game.Chat.Add("&cMissing chunk " + cx + ", " + cz);
					continue;
				} else {
					game.Chat.Add("&eImporting chunk " + cx + ", " + cz);
				}
				
				byte[] chunk = DecodeChunk(path);
				for (int yy = 0; yy < 128; yy++)
					for (int zz = 0, z = (cz - startZ) * 16; zz < 16; zz++)
						for (int xx = 0, x = (cx - startX) * 16; xx < 16; xx++)
				{
					blocks[(x + xx) + width * ((z + zz) + length * yy)] = chunk[yy + (zz * 128) + (xx * 128 * 16)];
				}
			}
			
			using (FileStream dst = File.Create("maps/world" + args[1] + ".cw")) {
				Save(dst, blocks, width, length);
			}			
			game.Chat.Add("&aImported all chunks to world" + args[1] + ".cw");
		}

		static void Save(Stream stream, byte[] blocks, int width, int length) {
			using (GZipStream wrapper = new GZipStream(stream, CompressionMode.Compress)) {
				BinaryWriter writer = new BinaryWriter(wrapper);
				NbtFile nbt = new NbtFile(writer);
				
				nbt.Write(NbtTagType.Compound); nbt.Write("ClassicWorld");
				nbt.Write(NbtTagType.Int8);
				nbt.Write("FormatVersion"); nbt.WriteUInt8(1);
				
				nbt.Write(NbtTagType.Int8Array);
				nbt.Write("UUID"); nbt.WriteInt32(16); nbt.WriteBytes(new byte[16]);
				nbt.Write(NbtTagType.Int16); nbt.Write("X"); nbt.WriteInt16((short)width);
				nbt.Write(NbtTagType.Int16); nbt.Write("Y"); nbt.WriteInt16((short)128);
				nbt.Write(NbtTagType.Int16); nbt.Write("Z"); nbt.WriteInt16((short)length);
				
				nbt.Write(NbtTagType.Compound); nbt.Write("Spawn");
				nbt.Write(NbtTagType.Int16); nbt.Write("X"); nbt.WriteInt16(0);
				nbt.Write(NbtTagType.Int16); nbt.Write("Y"); nbt.WriteInt16(0);
				nbt.Write(NbtTagType.Int16); nbt.Write("Z"); nbt.WriteInt16(0);
				nbt.Write(NbtTagType.Int8);  nbt.Write("H"); nbt.WriteUInt8(0);
				nbt.Write(NbtTagType.Int8);  nbt.Write("P"); nbt.WriteUInt8(0);
				nbt.Write(NbtTagType.End);
				
				nbt.Write(NbtTagType.Int8Array);
				nbt.Write("BlockArray"); nbt.WriteInt32(blocks.Length); nbt.WriteBytes(blocks);
				nbt.Write(NbtTagType.End);
			}
		}

		static byte[] DecodeChunk(string path) {
			using (Stream stream = File.OpenRead(path)) {
				using (Stream gs = new GZipStream(stream, CompressionMode.Decompress)) {
					BinaryReader reader = new BinaryReader(gs);
					if (reader.ReadByte() != (byte)NbtTagType.Compound)
						throw new InvalidDataException("Nbt file must start with Tag_Compound");
					
					NbtFile file = new NbtFile(reader);
					NbtTag root = file.ReadTag((byte)NbtTagType.Compound, true);
					NbtCompound children = (NbtCompound)root.Value;
					
					NbtCompound level = (NbtCompound)children["Level"].Value;
					return (byte[])level["Blocks"].Value;
				}
			}
		}
	}
}
