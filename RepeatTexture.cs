// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using BlockID = System.UInt16;
using ClassicalSharp;
using ClassicalSharp.Textures;
using OpenTK;

namespace AO {

	public sealed class Core : Plugin {		
		public int APIVersion { get { return 2; } }
		
		public static int[] RepeatX = new int[768];
		public static int[] RepeatY = new int[768];
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.ChunkUpdater.SetMeshBuilder(new RepeatTextureMeshBuilder());
			Events.BlockDefinitionChanged += CalcRepeats;
		}
		
		public void Ready(Game game) {
			game.ChunkUpdater.SetMeshBuilder(new RepeatTextureMeshBuilder());
			CalcRepeats();
		}
		
		public void Reset(Game game) {
			game.ChunkUpdater.SetMeshBuilder(new RepeatTextureMeshBuilder());
			CalcRepeats();
		}
		
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
		
		void CalcRepeats() {
			for (int i = 0; i < RepeatX.Length; i++) {
				RepeatX[i] = 1; RepeatY[i] = 1;
			}
			for (int i = 0; i < BlockInfo.MaxUsed; i++) {
				int start = BlockInfo.Name[i].IndexOf('[');
				if (start == -1) continue;
				int end = BlockInfo.Name[i].IndexOf(']');
				if (end <= start) continue;
				
				string coords = BlockInfo.Name[i].Substring(start + 1, end - start - 1);
				string[] XY = coords.Replace(" ", "").Split('x');
				RepeatX[i] = int.Parse(XY[0]);
				RepeatY[i] = int.Parse(XY[1]);
			}
		}
	}

	public unsafe sealed class RepeatTextureMeshBuilder : ChunkMeshBuilder {
		
		CuboidDrawer drawer = new CuboidDrawer();
		
		protected override int StretchXLiquid(int countIndex, int x, int y, int z, int chunkIndex, BlockID block) { return 1; }
		
		protected override int StretchX(int countIndex, int x, int y, int z, int chunkIndex, BlockID block, int face) { return 1; }
		
		protected override int StretchZ(int countIndex, int x, int y, int z, int chunkIndex, BlockID block, int face) { return 1; }
		
		protected override void RenderTile(int index) {
			if (BlockInfo.Draw[curBlock] == DrawType.Sprite) {
				this.fullBright = BlockInfo.FullBright[curBlock];
				this.tinted = BlockInfo.Tinted[curBlock];
				int count = counts[index + Side.Top];
				if (count != 0) DrawSprite(count);
				return;
			}
			
			int leftCount = counts[index++], rightCount = counts[index++],
			frontCount = counts[index++], backCount = counts[index++],
			bottomCount = counts[index++], topCount = counts[index++];
			if (leftCount == 0 && rightCount == 0 && frontCount == 0 &&
			    backCount == 0 && bottomCount == 0 && topCount == 0) return;
			
			bool fullBright = BlockInfo.FullBright[curBlock];
			bool isTranslucent = BlockInfo.Draw[curBlock] == DrawType.Translucent;
			int lightFlags = BlockInfo.LightOffset[curBlock];
			
			drawer.minBB = BlockInfo.MinBB[curBlock]; drawer.minBB.Y = 1 - drawer.minBB.Y;
			drawer.maxBB = BlockInfo.MaxBB[curBlock]; drawer.maxBB.Y = 1 - drawer.maxBB.Y;
			
			Vector3 min = BlockInfo.RenderMinBB[curBlock], max = BlockInfo.RenderMaxBB[curBlock];
			drawer.x1 = X + min.X; drawer.y1 = Y + min.Y; drawer.z1 = Z + min.Z;
			drawer.x2 = X + max.X; drawer.y2 = Y + max.Y; drawer.z2 = Z + max.Z;
			
			drawer.Tinted  = BlockInfo.Tinted[curBlock];
			drawer.TintCol = BlockInfo.FogCol[curBlock];
			
			if (leftCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Left];
				texLoc += Z % Core.RepeatX[curBlock]; texLoc += 16 * ((Y + 1) % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Left) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White :
					X >= offset ? light.LightCol_XSide_Fast(X - offset, Y, Z) : light.OutsideXSide;
				drawer.Left(leftCount, col, texLoc, vertices, ref part.vIndex[Side.Left]);
			}
			
			if (rightCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
				texLoc += (Z + 1) % Core.RepeatX[curBlock]; texLoc += 16 * ((Y + 1) % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Right) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White :
					X <= (maxX - offset) ? light.LightCol_XSide_Fast(X + offset, Y, Z) : light.OutsideXSide;
				drawer.Right(rightCount, col, texLoc, vertices, ref part.vIndex[Side.Right]);
			}
			
			if (frontCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Front];
				texLoc += (X + 1) % Core.RepeatX[curBlock]; texLoc += 16 * ((Y + 1) % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Front) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White :
					Z >= offset ? light.LightCol_ZSide_Fast(X, Y, Z - offset) : light.OutsideZSide;
				drawer.Front(frontCount, col, texLoc, vertices, ref part.vIndex[Side.Front]);
			}
			
			if (backCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Back];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * ((Y + 1) % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Back) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White :
					Z <= (maxZ - offset) ? light.LightCol_ZSide_Fast(X, Y, Z + offset) : light.OutsideZSide;
				drawer.Back(backCount, col, texLoc, vertices, ref part.vIndex[Side.Back]);
			}
			
			if (bottomCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Bottom];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Z % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Bottom) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White : light.LightCol_YBottom_Fast(X, Y - offset, Z);
				drawer.Bottom(bottomCount, col, texLoc,vertices, ref part.vIndex[Side.Bottom]);
			}
			
			if (topCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Top];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Z % Core.RepeatY[curBlock]);
				int i = texLoc >> Atlas1D.Shift;
				int offset = (lightFlags >> Side.Top) & 1;

				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				PackedCol col = fullBright ? PackedCol.White : light.LightCol_YTop_Fast(X, (Y + 1) - offset, Z);
				drawer.Top(topCount, col, texLoc, vertices, ref part.vIndex[Side.Top]);
			}
		}
	}
}