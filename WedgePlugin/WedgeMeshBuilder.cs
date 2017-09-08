// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using OpenTK;

namespace ClassicalSharp {

	
	public static class DrawType2 {
		public const byte SlopeUpXMin = 6;
		public const byte SlopeUpXMax = 7;
		public const byte SlopeUpZMin = 8;
		public const byte SlopeUpZMax = 9;
		public const byte SlopeDownXMin = 10;
		public const byte SlopeDownXMax = 11;
		public const byte SlopeDownZMin = 12;
		public const byte SlopeDownZMax = 13;
	}
	
	public unsafe sealed class WedgeMeshBuilder : ChunkMeshBuilder  {
		
		protected override int StretchXLiquid(int countIndex, int x, int y, int z, int chunkIndex, byte block) {
			if (OccludedLiquid(chunkIndex)) return 0;
			int count = 1;
			x++;
			chunkIndex++;
			countIndex += Side.Sides;
			bool stretchTile = (BlockInfo.CanStretch[block] & (1 << Side.Top)) != 0;
			
			while (x < chunkEndX && stretchTile && CanStretch(block, chunkIndex, x, y, z, Side.Top) && !OccludedLiquid(chunkIndex)) {
				counts[countIndex] = 0;
				count++;
				x++;
				chunkIndex++;
				countIndex += Side.Sides;
			}
			return count;
		}
		
		protected override int StretchX(int countIndex, int x, int y, int z, int chunkIndex, byte block, int face) {
			int count = 1;
			x++;
			chunkIndex++;
			countIndex += Side.Sides;
			bool stretchTile = (BlockInfo.CanStretch[block] & (1 << face)) != 0;
			
			while (x < chunkEndX && stretchTile && CanStretch(block, chunkIndex, x, y, z, face)) {
				counts[countIndex] = 0;
				count++;
				x++;
				chunkIndex++;
				countIndex += Side.Sides;
			}
			return count;
		}
		
		protected override int StretchZ(int countIndex, int x, int y, int z, int chunkIndex, byte block, int face) {
			int count = 1;
			z++;
			chunkIndex += extChunkSize;
			countIndex += chunkSize * Side.Sides;
			bool stretchTile = (BlockInfo.CanStretch[block] & (1 << face)) != 0;
			
			while (z < chunkEndZ && stretchTile && CanStretch(block, chunkIndex, x, y, z, face)) {
				counts[countIndex] = 0;
				count++;
				z++;
				chunkIndex += extChunkSize;
				countIndex += chunkSize * Side.Sides;
			}
			return count;
		}
		
		bool CanStretch(byte initial, int chunkIndex, int x, int y, int z, int face) {
			byte cur = chunk[chunkIndex];
			return cur == initial
				&& !BlockInfo.IsFaceHidden(cur, chunk[chunkIndex + offsets[face]], face)
				&& (fullBright || (LightCol(X, Y, Z, face, initial) == LightCol(x, y, z, face, cur)));
		}
		
		int LightCol(int x, int y, int z, int face, byte block) {
			int offset = (BlockInfo.LightOffset[block] >> face) & 1;
			switch (face) {
				case Side.Left:
					return x < offset          ? light.OutsideXSide    : light.LightCol_XSide_Fast(x - offset, y, z);
				case Side.Right:
					return x > (maxX - offset) ? light.OutsideXSide   : light.LightCol_XSide_Fast(x + offset, y, z);
				case Side.Front:
					return z < offset          ? light.OutsideZSide   : light.LightCol_ZSide_Fast(x, y, z - offset);
				case Side.Back:
					return z > (maxZ - offset) ? light.OutsideZSide   : light.LightCol_ZSide_Fast(x, y, z + offset);
				case Side.Bottom:
					return y <= 0              ? light.OutsideYBottom : light.LightCol_YBottom_Fast(x, y - offset, z);
				case Side.Top:
					return y >= maxY           ? light.Outside        : light.LightCol_YTop_Fast(x, (y + 1) - offset, z);
			}
			return 0;
		}
		
		void DrawLeftFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Left];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Left) & 1;
			
			float u1 = minBB.Z, u2 = (count - 1) + maxBB.Z * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked :
				X >= offset ? light.LightCol_XSide_Fast(X - offset, Y, Z) : light.OutsideXSide;
			if (tinted) col = TintBlock(curBlock, col);
			
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMin || BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMin) {
				part.vertices[part.vIndex[Side.Left]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Left]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Left]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Left]++] = default(VertexP3fT2fC4b);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMin) {
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMax) {
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMin) {
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMax) {
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col);
			} else {
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col);
			}
		}

		void DrawRightFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Right) & 1;
			
			float u1 = (count - minBB.Z), u2 = (1 - maxBB.Z) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked :
				X <= (maxX - offset) ? light.LightCol_XSide_Fast(X + offset, Y, Z) : light.OutsideXSide;
			if (tinted) col = TintBlock(curBlock, col);
			
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMax || BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMax) {
				part.vertices[part.vIndex[Side.Right]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Right]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Right]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Right]++] = default(VertexP3fT2fC4b);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMin) {
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMax) {
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMin) {
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMax) {
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col);
			} else {
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col);
				part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col);
			}
		}

		void DrawFrontFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Front];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Front) & 1;
			
			float u1 = (count - minBB.X), u2 = (1 - maxBB.X) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked :
				Z >= offset ? light.LightCol_ZSide_Fast(X, Y, Z - offset) : light.OutsideZSide;
			if (tinted) col = TintBlock(curBlock, col);
			
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMin || BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMin) {
				part.vertices[part.vIndex[Side.Front]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Front]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Front]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Front]++] = default(VertexP3fT2fC4b);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMin) {
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMax) {
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMin) {
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMax) {
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v1, col);
			} else {
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col);
				part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col);
			}
		}
		
		void DrawBackFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Back];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Back) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked :
				Z <= (maxZ - offset) ? light.LightCol_ZSide_Fast(X, Y, Z + offset) : light.OutsideZSide;
			if (tinted) col = TintBlock(curBlock, col);
			
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMax || BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMax) {
				part.vertices[part.vIndex[Side.Back]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Back]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Back]++] = default(VertexP3fT2fC4b);
				part.vertices[part.vIndex[Side.Back]++] = default(VertexP3fT2fC4b);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMin) {
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMax) {
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMin) {
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMax) {
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col);
			} else {
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col);
				part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col);
			}
		}
		
		void DrawBottomFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Bottom];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Bottom) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked : light.LightCol_YBottom_Fast(X, Y - offset, Z);
			if (tinted) col = TintBlock(curBlock, col);
			
			float y1_x1z1 = y1, y1_x1z2 = y1, y1_x2z1 = y1, y1_x2z2 = y1;
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMax) {
				y1_x2z1 = y2; y1_x2z2 = y2;
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpXMin) {
				y1_x1z1 = y2; y1_x1z2 = y2;
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMin) {
				y1_x1z1 = y2; y1_x2z1 = y2;
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeUpZMax) {
				y1_x1z2 = y2; y1_x2z2 = y2;
			}
			
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1_x2z2, z2, u2, v2, col);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x1, y1_x1z2, z2, u1, v2, col);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x1, y1_x1z1, z1, u1, v1, col);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1_x2z1, z1, u2, v1, col);
		}

		void DrawTopFace(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Top];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Top) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			int col = fullBright ? FastColour.WhitePacked : light.LightCol_YTop_Fast(X, (Y + 1) - offset, Z);
			if (tinted) col = TintBlock(curBlock, col);

			float y2_x1z1 = y2, y2_x1z2 = y2, y2_x2z1 = y2, y2_x2z2 = y2;
			if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMax) {
				y2_x2z1 = y1; y2_x2z2 = y1;
				if (!fullBright) col = FastColour.ScalePacked(col, 0.8f);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownXMin) {
				y2_x1z1 = y1; y2_x1z2 = y1;
				if (!fullBright) col = FastColour.ScalePacked(col, 0.8f);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMin) {
				y2_x1z1 = y1; y2_x2z1 = y1;
				if (!fullBright) col = FastColour.ScalePacked(col, 0.9f);
			} else if (BlockInfo.Draw[curBlock] == DrawType2.SlopeDownZMax) {
				y2_x1z2 = y1; y2_x2z2 = y1;
				if (!fullBright) col = FastColour.ScalePacked(col, 0.9f);
			}
			
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2_x2z1, z1, u2, v1, col);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x1, y2_x1z1, z1, u1, v1, col);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x1, y2_x1z2, z2, u1, v2, col);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2_x2z2, z2, u2, v2, col);
		}
		
		float x1, y1, z1, x2, y2, z2;
		Vector3 minBB, maxBB;
		bool isTranslucent;
		byte lightFlags;
		
		protected override void RenderTile(int index) {
			this.fullBright = BlockInfo.FullBright[curBlock];
			this.tinted = BlockInfo.Tinted[curBlock];
			
			if (BlockInfo.Draw[curBlock] == DrawType.Sprite) {
				int count = counts[index + Side.Top];
				if (count != 0) DrawSprite(count);
				return;
			}
			
			int leftCount = counts[index++], rightCount = counts[index++],
			frontCount = counts[index++], backCount = counts[index++],
			bottomCount = counts[index++], topCount = counts[index++];
			if (leftCount == 0 && rightCount == 0 && frontCount == 0 &&
			    backCount == 0 && bottomCount == 0 && topCount == 0) return;
			
			minBB = BlockInfo.MinBB[curBlock]; minBB.Y = 1 - minBB.Y;
			maxBB = BlockInfo.MaxBB[curBlock]; maxBB.Y = 1 - maxBB.Y;
			
			Vector3 min = BlockInfo.RenderMinBB[curBlock];
			Vector3 max = BlockInfo.RenderMaxBB[curBlock];
			x1 = X + min.X; y1 = Y + min.Y; z1 = Z + min.Z;
			x2 = X + max.X; y2 = Y + max.Y; z2 = Z + max.Z;
			isTranslucent = BlockInfo.Draw[curBlock] == DrawType.Translucent;
			lightFlags = BlockInfo.LightOffset[curBlock];
			
			if (leftCount != 0) DrawLeftFace(leftCount);
			if (rightCount != 0) DrawRightFace(rightCount);
			if (frontCount != 0) DrawFrontFace(frontCount);
			if (backCount != 0) DrawBackFace(backCount);
			if (bottomCount != 0) DrawBottomFace(bottomCount);
			if (topCount != 0) DrawTopFace(topCount);
		}
		
	}
}
