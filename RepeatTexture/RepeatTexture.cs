// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
#if USE16_BIT
using BlockID = System.UInt16;
#else
using BlockID = System.Byte;
#endif
using ClassicalSharp;
using OpenTK;

namespace AO {

	public sealed class Core : Plugin {
		
		public string ClientVersion { get { return "0.99.4"; } }
		
		public static int[] RepeatX = new int[Block.Count];
		public static int[] RepeatY = new int[Block.Count];
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.MapRenderer.SetMeshBuilder(new RepeatTextureMeshBuilder());
			game.Events.BlockDefinitionChanged += BlockDefinitionChanged;
		}

		void BlockDefinitionChanged(object sender, EventArgs e) {
			CalcRepeats();
		}
		
		public void Ready(Game game) {
			game.MapRenderer.SetMeshBuilder(new RepeatTextureMeshBuilder());
			CalcRepeats();
		}
		
		public void Reset(Game game) {
			game.MapRenderer.SetMeshBuilder(new RepeatTextureMeshBuilder());
			CalcRepeats();
		}
		
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
		
		void CalcRepeats() {
			for (int i = 0; i < Block.Count; i++) {
				RepeatX[i] = 1; RepeatY[i] = 1;
				
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
		
		protected override void PreStretchTiles(int x1, int y1, int z1) {
			base.PreStretchTiles(x1, y1, z1);
			drawer.invVerElementSize = invVerElementSize;
			drawer.elementsPerAtlas1D = elementsPerAtlas1D;
		}
		
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
			
			drawer.Tinted = BlockInfo.Tinted[curBlock];
			drawer.TintColour = BlockInfo.FogColour[curBlock];
			
			if (leftCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Left];
				texLoc += Z % Core.RepeatX[curBlock]; texLoc += 16 * (Y % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Left) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked :
					X >= offset ? light.LightCol_XSide_Fast(X - offset, Y, Z) : light.OutsideXSide;
				drawer.Left(leftCount, col, texLoc, part.vertices, ref part.vIndex[Side.Left]);
			}
			
			if (rightCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
				texLoc += Z % Core.RepeatX[curBlock]; texLoc += 16 * (Y % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Right) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked :
					X <= (maxX - offset) ? light.LightCol_XSide_Fast(X + offset, Y, Z) : light.OutsideXSide;
				drawer.Right(rightCount, col, texLoc, part.vertices, ref part.vIndex[Side.Right]);
			}
			
			if (frontCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Front];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Y % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Front) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked :
					Z >= offset ? light.LightCol_ZSide_Fast(X, Y, Z - offset) : light.OutsideZSide;
				drawer.Front(frontCount, col, texLoc, part.vertices, ref part.vIndex[Side.Front]);
			}
			
			if (backCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Back];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Y % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Back) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked :
					Z <= (maxZ - offset) ? light.LightCol_ZSide_Fast(X, Y, Z + offset) : light.OutsideZSide;
				drawer.Back(backCount, col, texLoc, part.vertices, ref part.vIndex[Side.Back]);
			}
			
			if (bottomCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Bottom];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Z % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Bottom) & 1;
				
				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked : light.LightCol_YBottom_Fast(X, Y - offset, Z);
				drawer.Bottom(bottomCount, col, texLoc, part.vertices, ref part.vIndex[Side.Bottom]);
			}
			
			if (topCount != 0) {
				int texLoc = BlockInfo.textures[curBlock * Side.Sides + Side.Top];
				texLoc += X % Core.RepeatX[curBlock]; texLoc += 16 * (Z % Core.RepeatY[curBlock]);
				int i = texLoc / elementsPerAtlas1D;
				int offset = (lightFlags >> Side.Top) & 1;

				DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
				int col = fullBright ? FastColour.WhitePacked : light.LightCol_YTop_Fast(X, (Y + 1) - offset, Z);
				drawer.Top(topCount, col, texLoc, part.vertices, ref part.vIndex[Side.Top]);
			}
		}
	}
}