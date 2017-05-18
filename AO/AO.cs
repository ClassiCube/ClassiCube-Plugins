// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using OpenTK;

namespace AO {
	
	public unsafe sealed class AOMeshBuilder : ChunkMeshBuilder {
		
		bool[] isOccluder = new bool[Block.Count];
		FastColour sun, sunX, sunZ, sunYBottom;
		FastColour dark, darkX, darkZ, darkYBottom;
		
		protected override void PostStretchTiles(int x1, int y1, int z1) {
			base.PostStretchTiles(x1, y1, z1);
			for (int i = 0; i < isOccluder.Length; i++) {
				isOccluder[i] =
					info.BlocksLight[i] &&
					info.MinBB[i] == Vector3.Zero &&
					info.MaxBB[i] == Vector3.One &&
					info.Draw[i] != DrawType.TransparentThick; // goodlyay, did you hack this for leaves?
			}
			
			sun = env.Sunlight;
			sunX = FastColour.Unpack(env.SunXSide);
			sunZ = FastColour.Unpack(env.SunZSide);
			sunYBottom = FastColour.Unpack(env.SunYBottom);
			
			dark = FastColour.Unpack(FastColour.ScalePacked(env.Shadow, 0.3f));
			darkX = FastColour.Unpack(FastColour.ScalePacked(env.ShadowXSide, 0.3f));
			darkZ = FastColour.Unpack(FastColour.ScalePacked(env.ShadowZSide, 0.3f));
			darkYBottom = FastColour.Unpack(FastColour.ScalePacked(env.ShadowYBottom, 0.3f));
		}
		
		Vector3 minBB, maxBB;
		bool isTranslucent;
		int lightFlags;
		float x1, y1, z1, x2, y2, z2;
		
		protected override int StretchXLiquid(int countIndex, int x, int y, int z, int chunkIndex, byte block) {
			return 1;
		}
		
		protected override int StretchX(int countIndex, int x, int y, int z, int chunkIndex, byte block, int face) {
			return 1;
		}
		
		protected override int StretchZ(int countIndex, int x, int y, int z, int chunkIndex, byte block, int face) {
			return 1;
		}
		
		bool CanStretch(byte initialTile, int chunkIndex, int x, int y, int z, int face) {
			return false;
		}
		
		
		protected override void RenderTile(int index) {
			if (info.Draw[curBlock] == DrawType.Sprite) {
				fullBright = info.FullBright[curBlock];
				tinted = info.Tinted[curBlock];
				int count = counts[index + Side.Top];
				if (count != 0) DrawSprite(count);
				return;
			}
			
			int leftCount = counts[index++], rightCount = counts[index++],
			frontCount = counts[index++], backCount = counts[index++],
			bottomCount = counts[index++], topCount = counts[index++];
			if (leftCount == 0 && rightCount == 0 && frontCount == 0 &&
			    backCount == 0 && bottomCount == 0 && topCount == 0) return;
			
			fullBright = info.FullBright[curBlock];
			isTranslucent = info.Draw[curBlock] == DrawType.Translucent;
			lightFlags = info.LightOffset[curBlock];
			tinted = info.Tinted[curBlock];
			
			Vector3 min = info.RenderMinBB[curBlock], max = info.RenderMaxBB[curBlock];
			x1 = X + min.X; y1 = Y + min.Y; z1 = Z + min.Z;
			x2 = X + max.X; y2 = Y + max.Y; z2 = Z + max.Z;
			
			this.minBB = info.MinBB[curBlock]; this.maxBB = info.MaxBB[curBlock];
			minBB.Y = 1 - minBB.Y; maxBB.Y = 1 - maxBB.Y;
			
			if (leftCount != 0) DrawLeftFace(leftCount);
			if (rightCount != 0) DrawRightFace(rightCount);
			if (frontCount != 0) DrawFrontFace(frontCount);
			if (backCount != 0) DrawBackFace(backCount);
			if (bottomCount != 0) DrawBottomFace(bottomCount);
			if (topCount != 0) DrawTopFace(topCount);
		}
		
		
		void DrawLeftFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Left];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Left) & 1;
			
			float u1 = minBB.Z, u2 = (count - 1) + maxBB.Z * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			int col0_0 = AverageColorsXSide(X-offset, Y, Z, -1, -1);
			int col0_1 = AverageColorsXSide(X-offset, Y, Z, -1, +1);
			int col1_1 = AverageColorsXSide(X-offset, Y, Z, +1, +1);
			int col1_0 = AverageColorsXSide(X-offset, Y, Z, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z2 + (count - 1), u2, v1, col1_1);
			part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col0_1);
			part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col0_0);
			part.vertices[part.vIndex[Side.Left]++] = new VertexP3fT2fC4b(x1, y1, z2 + (count - 1), u2, v2, col1_0);
		}
		
		void DrawRightFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Right];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Right) & 1;
			
			float u1 = (count - minBB.Z), u2 = (1 - maxBB.Z) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			int col0_0 = AverageColorsXSide(X+offset, Y, Z, -1, -1);
			int col0_1 = AverageColorsXSide(X+offset, Y, Z, -1, +1);
			int col1_1 = AverageColorsXSide(X+offset, Y, Z, +1, +1);
			int col1_0 = AverageColorsXSide(X+offset, Y, Z, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z2 + (count - 1), u2, v1, col1_1);
			part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z2 + (count - 1), u2, v2, col1_0);
			part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y1, z1, u1, v2, col0_0);
			part.vertices[part.vIndex[Side.Right]++] = new VertexP3fT2fC4b(x2, y2, z1, u1, v1, col0_1);
		}
		
		void DrawFrontFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Front];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Front) & 1;
			
			float u1 = (count - minBB.X), u2 = (1 - maxBB.X) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			int col0_0 = AverageColorsZSide(X, Y, Z-offset, -1, -1);
			int col0_1 = AverageColorsZSide(X, Y, Z-offset, -1, +1);
			int col1_1 = AverageColorsZSide(X, Y, Z-offset, +1, +1);
			int col1_0 = AverageColorsZSide(X, Y, Z-offset, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v2, col0_0);
			part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col0_1);
			part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col1_1);
			part.vertices[part.vIndex[Side.Front]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v2, col1_0);
		}
		
		void DrawBackFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Back];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Back) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			int col0_0 = AverageColorsZSide(X, Y, Z+offset, -1, -1);
			int col0_1 = AverageColorsZSide(X, Y, Z+offset, -1, +1);
			int col1_1 = AverageColorsZSide(X, Y, Z+offset, +1, +1);
			int col1_0 = AverageColorsZSide(X, Y, Z+offset, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v1, col1_1);
			part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v1, col0_1);
			part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col0_0);
			part.vertices[part.vIndex[Side.Back]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col1_0);
		}
		
		void DrawBottomFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Bottom];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Bottom) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			int col0_0 = AverageColorsBottom(X, Y-offset, Z, -1, -1);
			int col0_1 = AverageColorsBottom(X, Y-offset, Z, -1, +1);
			int col1_1 = AverageColorsBottom(X, Y-offset, Z, +1, +1);
			int col1_0 = AverageColorsBottom(X, Y-offset, Z, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x1, y1, z2, u1, v2, col0_1);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x1, y1, z1, u1, v1, col0_0);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z1, u2, v1, col1_0);
			part.vertices[part.vIndex[Side.Bottom]++] = new VertexP3fT2fC4b(x2 + (count - 1), y1, z2, u2, v2, col1_1);
		}
		
		void DrawTopFace(int count) {
			int texId = info.textures[curBlock * Side.Sides + Side.Top];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			int offset = (lightFlags >> Side.Top) & 1;
			
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
			
			
			if (info.MinBB[curBlock].Y > 0 && info.MaxBB[curBlock].Y == 1) { offset = 1; }
			
			int col0_0 = AverageColorsTop(X, Y+offset, Z, -1, -1);
			int col0_1 = AverageColorsTop(X, Y+offset, Z, -1, +1);
			int col1_1 = AverageColorsTop(X, Y+offset, Z, +1, +1);
			int col1_0 = AverageColorsTop(X, Y+offset, Z, +1, -1);
			
			if (tinted) {
				col0_0 = TintBlock(curBlock, col0_0);
				col1_0 = TintBlock(curBlock, col1_0);
				col1_1 = TintBlock(curBlock, col1_1);
				col0_1 = TintBlock(curBlock, col0_1);
			}
			
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x1, y2, z1, u1, v1, col0_0);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x1, y2, z2, u1, v2, col0_1);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z2, u2, v2, col1_1);
			part.vertices[part.vIndex[Side.Top]++] = new VertexP3fT2fC4b(x2 + (count - 1), y2, z1, u2, v1, col1_0);
		}
		
		int AverageColorsTop(int X, int Y, int Z, int dX, int dZ) {
			if (fullBright) return FastColour.WhitePacked;
			bool useDiagonal = true;
			bool block1;
			bool block2;
			bool dummy;
			int R;
			int G;
			int B;
			FastColour col1 = GetBlockColorTop(X, Y, Z, out dummy);
			FastColour col2 = GetBlockColorTop(X+dX, Y, Z, out block1);
			FastColour col3 = GetBlockColorTop(X, Y, Z+dZ, out block2);
			FastColour col4 = GetBlockColorTop(X+dX, Y, Z+dZ, out dummy);
			
			useDiagonal = !(block1 && block2);
			if (useDiagonal) {
				R = (col1.R + col2.R + col3.R + col4.R) / 4;
				G = (col1.G + col2.G + col3.G + col4.G) / 4;
				B = (col1.B + col2.B + col3.B + col4.B) / 4;
			} else {
				R = (col1.R + col2.R + col3.R) / 3;
				G = (col1.G + col2.G + col3.G) / 3;
				B = (col1.B + col2.B + col3.B) / 3;
			}
			
			return new FastColour(R, G, B).Pack();
		}
		
		FastColour GetBlockColorTop(int X, int Y, int Z, out bool blocksLight) {
			blocksLight = false;
			if (map.IsValidPos(X, Y, Z)) {
				byte thisBlock = map.GetBlock(X, Y, Z);
				if (isOccluder[thisBlock]) blocksLight = true;
				
				FastColour col = blocksLight ? dark : FastColour.Unpack(light.LightCol(X, Y, Z));
				if (info.FullBright[thisBlock]) col = sun;
				return col;
			}
			return sun;
		}
		
		int AverageColorsBottom(int X, int Y, int Z, int dX, int dZ) {
			if (fullBright) return FastColour.WhitePacked;
			bool useDiagonal = true;
			bool block1;
			bool block2;
			bool dummy;
			int R;
			int G;
			int B;
			FastColour col1 = GetBlockColorBottom(X, Y, Z, out dummy);
			FastColour col2 = GetBlockColorBottom(X+dX, Y, Z, out block1);
			FastColour col3 = GetBlockColorBottom(X, Y, Z+dZ, out block2);
			FastColour col4 = GetBlockColorBottom(X+dX, Y, Z+dZ, out dummy);
			
			useDiagonal = !(block1 && block2);
			if (useDiagonal) {
				R = (col1.R + col2.R + col3.R + col4.R) / 4;
				G = (col1.G + col2.G + col3.G + col4.G) / 4;
				B = (col1.B + col2.B + col3.B + col4.B) / 4;
			} else {
				R = (col1.R + col2.R + col3.R) / 3;
				G = (col1.G + col2.G + col3.G) / 3;
				B = (col1.B + col2.B + col3.B) / 3;
			}
			
			return new FastColour(R, G, B).Pack();
		}
		
		FastColour GetBlockColorBottom(int X, int Y, int Z, out bool blocksLight) {
			blocksLight = false;
			if (map.IsValidPos(X, Y, Z)) {
				byte thisBlock = map.GetBlock(X, Y, Z);
				if (isOccluder[thisBlock]) blocksLight = true;
				
				FastColour col = blocksLight ? darkYBottom : FastColour.Unpack(light.LightCol_YBottom_Fast(X, Y, Z));
				if (info.FullBright[thisBlock]) col = sun;
				return col;
			}
			return sunYBottom;
		}
		
		
		int AverageColorsZSide(int X, int Y, int Z, int dX, int dY) {
			if (fullBright) return FastColour.WhitePacked;
			bool useDiagonal = true;
			bool block1;
			bool block2;
			bool dummy;
			int R;
			int G;
			int B;
			FastColour col1 = GetBlockColorZSide(X, Y, Z, out dummy);
			FastColour col2 = GetBlockColorZSide(X+dX, Y, Z, out block1);
			FastColour col3 = GetBlockColorZSide(X, Y+dY, Z, out block2);
			FastColour col4 = GetBlockColorZSide(X+dX, Y+dY, Z, out dummy);
			
			useDiagonal = !(block1 && block2);
			if (useDiagonal) {
				R = (col1.R + col2.R + col3.R + col4.R) / 4;
				G = (col1.G + col2.G + col3.G + col4.G) / 4;
				B = (col1.B + col2.B + col3.B + col4.B) / 4;
			} else {
				R = (col1.R + col2.R + col3.R) / 3;
				G = (col1.G + col2.G + col3.G) / 3;
				B = (col1.B + col2.B + col3.B) / 3;
			}
			
			return new FastColour(R, G, B).Pack();
		}
		
		FastColour GetBlockColorZSide(int X, int Y, int Z, out bool blocksLight) {
			blocksLight = false;
			if (map.IsValidPos(X, Y, Z)) {
				byte thisBlock = map.GetBlock(X, Y, Z);
				if (isOccluder[thisBlock]) blocksLight = true;
				
				FastColour col = blocksLight ? darkZ : FastColour.Unpack(light.LightCol_ZSide_Fast(X, Y, Z));
				if (info.FullBright[thisBlock]) col = sun;
				return col;
			}
			return sunZ;
		}
		
		
		int AverageColorsXSide(int X, int Y, int Z, int dZ, int dY) {
			if (fullBright) return FastColour.WhitePacked;
			bool useDiagonal = true;
			bool block1;
			bool block2;
			bool dummy;
			int R;
			int G;
			int B;
			FastColour col1 = GetBlockColorXSide(X, Y, Z, out dummy);
			FastColour col2 = GetBlockColorXSide(X, Y, Z+dZ, out block1);
			FastColour col3 = GetBlockColorXSide(X, Y+dY, Z, out block2);
			FastColour col4 = GetBlockColorXSide(X, Y+dY, Z+dZ, out dummy);
			
			useDiagonal = !(block1 && block2);
			if (useDiagonal) {
				R = (col1.R + col2.R + col3.R + col4.R) / 4;
				G = (col1.G + col2.G + col3.G + col4.G) / 4;
				B = (col1.B + col2.B + col3.B + col4.B) / 4;
			} else {
				R = (col1.R + col2.R + col3.R) / 3;
				G = (col1.G + col2.G + col3.G) / 3;
				B = (col1.B + col2.B + col3.B) / 3;
			}
			
			return new FastColour(R, G, B).Pack();
		}
		
		FastColour GetBlockColorXSide(int X, int Y, int Z, out bool blocksLight) {
			blocksLight = false;
			if (map.IsValidPos(X, Y, Z)) {
				byte thisBlock = map.GetBlock(X, Y, Z);
				if (isOccluder[thisBlock]) blocksLight = true;
				
				FastColour col = blocksLight ? darkX : FastColour.Unpack(light.LightCol_XSide_Fast(X, Y, Z));
				if (info.FullBright[thisBlock]) col = sun;
				return col;
			}
			return sunX;
		}
	}
}
