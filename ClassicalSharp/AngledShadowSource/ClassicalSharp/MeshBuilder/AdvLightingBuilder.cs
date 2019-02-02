// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using OpenTK;

#if USE16_BIT
using BlockID = System.UInt16;
#else
using BlockID = System.Byte;
#endif
 
namespace ClassicalSharp {
 
    public unsafe sealed class AdvLightingMeshBuilder : ChunkMeshBuilder {
       
        bool[] isOccluder = new bool[Block.Count];
        FastColour sun, sunX, sunZ, sunYBottom;
        FastColour dark, darkX, darkZ, darkYBottom;
       
        protected override void PostStretchTiles(int x1, int y1, int z1) {
            base.PostStretchTiles(x1, y1, z1);
            for (int i = 0; i < isOccluder.Length; i++) {
                isOccluder[i] =
                    BlockInfo.BlocksLight[i] &&
                    BlockInfo.MinBB[i] == Vector3.Zero &&
                    BlockInfo.MaxBB[i] == Vector3.One &&
                    i != 8 && i != 9 &&
                    BlockInfo.Draw[i] != DrawType.TransparentThick; // goodlyay, did you hack this for leaves?
                    //(BlockInfo.Draw[i] != DrawType.TransparentThick && BlockInfo.Draw[i] != DrawType.Translucent); // goodlyay, did you hack this for leaves?
            }
            
            sun = env.Sunlight;
            sunX = FastColour.Unpack(env.SunXSide);
            sunZ = FastColour.Unpack(env.SunZSide);
            sunYBottom = FastColour.Unpack(env.SunYBottom);
            
            float darkness = 0.4f;
            
            dark = FastColour.Unpack(FastColour.ScalePacked(env.Shadow, darkness));
            darkX = FastColour.Unpack(FastColour.ScalePacked(env.ShadowXSide, darkness));
            darkZ = FastColour.Unpack(FastColour.ScalePacked(env.ShadowZSide, darkness));
            darkYBottom = FastColour.Unpack(FastColour.ScalePacked(env.ShadowYBottom, darkness));
        }
       
        Vector3 minBB, maxBB;
        bool isTranslucent;
        int initBitFlags, lightFlags;
        float x1, y1, z1, x2, y2, z2;
       
		protected override int StretchXLiquid(int countIndex, int x, int y, int z, int chunkIndex, BlockID block) {
            return 1;
		}
       
		protected override int StretchX(int countIndex, int x, int y, int z, int chunkIndex, BlockID block, int face) {
            return 1;
		}
		
		protected override int StretchZ(int countIndex, int x, int y, int z, int chunkIndex, BlockID block, int face) {
            return 1;
		}
       
        bool CanStretch(BlockID initialTile, int chunkIndex, int x, int y, int z, int face) {
            BlockID rawBlock = chunk[chunkIndex];
            bitFlags[chunkIndex] = ComputeLightFlags(x, y, z, chunkIndex);
            return rawBlock == initialTile
                && !BlockInfo.IsFaceHidden(rawBlock, chunk[chunkIndex + offsets[face]], face)
                && (initBitFlags == bitFlags[chunkIndex]
                    // Check that this face is either fully bright or fully in shadow
                    && (initBitFlags == 0 || (initBitFlags & masks[face]) == masks[face]));
        }
       
       
        protected override void RenderTile(int index) {
            if (BlockInfo.Draw[curBlock] == DrawType.Sprite) {
                fullBright = BlockInfo.FullBright[curBlock];
                tinted = BlockInfo.Tinted[curBlock];
                int count = counts[index + Side.Top];
                if (count != 0) DrawSprite(count);
                return;
            }
           
            int leftCount = counts[index++], rightCount = counts[index++],
            frontCount = counts[index++], backCount = counts[index++],
            bottomCount = counts[index++], topCount = counts[index++];
            if (leftCount == 0 && rightCount == 0 && frontCount == 0 &&
                backCount == 0 && bottomCount == 0 && topCount == 0) return;
           
            fullBright = BlockInfo.FullBright[curBlock];
            isTranslucent = BlockInfo.Draw[curBlock] == DrawType.Translucent;
            lightFlags = BlockInfo.LightOffset[curBlock];
            tinted = BlockInfo.Tinted[curBlock];
           
            Vector3 min = BlockInfo.RenderMinBB[curBlock], max = BlockInfo.RenderMaxBB[curBlock];
            x1 = X + min.X; y1 = Y + min.Y; z1 = Z + min.Z;
            x2 = X + max.X; y2 = Y + max.Y; z2 = Z + max.Z;
           
            this.minBB = BlockInfo.MinBB[curBlock]; this.maxBB = BlockInfo.MaxBB[curBlock];
            minBB.Y = 1 - minBB.Y; maxBB.Y = 1 - maxBB.Y;
           
            if (leftCount != 0) DrawLeftFace(leftCount);
            if (rightCount != 0) DrawRightFace(rightCount);
            if (frontCount != 0) DrawFrontFace(frontCount);
            if (backCount != 0) DrawBackFace(backCount);
            if (bottomCount != 0) DrawBottomFace(bottomCount);
            if (topCount != 0) DrawTopFace(topCount);
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
            int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
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
            int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Front];
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
            int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Back];
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
            int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Bottom];
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
            int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Top];
            int i = texId / elementsPerAtlas1D;
            float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
            int offset = (lightFlags >> Side.Top) & 1;
 
            float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
            float v1 = vOrigin + minBB.Z * invVerElementSize;
            float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
            DrawInfo part = isTranslucent ? translucentParts[i] : normalParts[i];
           
           
            if (BlockInfo.MinBB[curBlock].Y > 0 && BlockInfo.MaxBB[curBlock].Y == 1) { offset = 1; }
            offset = 1;
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
        
		protected override void DrawSprite(int count) {
			int texId = BlockInfo.textures[curBlock * Side.Sides + Side.Right];
			int i = texId / elementsPerAtlas1D;
			float vOrigin = (texId % elementsPerAtlas1D) * invVerElementSize;
			const float blockHeight = 1;
			
			const float u1 = 0, u2 = 15.99f/16f;
			float v1 = vOrigin, v2 = vOrigin + invVerElementSize * 15.99f/16f;
			DrawInfo part = normalParts[i];
			int col = fullBright ? FastColour.WhitePacked : light.LightCol_Sprite_Fast(X, Y, Z);
			if (tinted) col = TintBlock(curBlock, col);
			
            int col0_0 = AverageColorsTop(X, Y, Z, -1, -1);
            int col0_1 = AverageColorsTop(X, Y, Z, -1, +1);
            int col1_1 = AverageColorsTop(X, Y, Z, +1, +1);
            int col1_0 = AverageColorsTop(X, Y, Z, +1, -1);
            
            int col0_0T = AverageColorsTop(X, Y+1, Z, -1, -1);
            int col0_1T = AverageColorsTop(X, Y+1, Z, -1, +1);
            int col1_1T = AverageColorsTop(X, Y+1, Z, +1, +1);
            int col1_0T = AverageColorsTop(X, Y+1, Z, +1, -1);
           
            if (tinted) {
                col0_0 = TintBlock(curBlock, col0_0);
                col1_0 = TintBlock(curBlock, col1_0);
                col1_1 = TintBlock(curBlock, col1_1);
                col0_1 = TintBlock(curBlock, col0_1);
                col0_0T = TintBlock(curBlock, col0_0T);
                col1_0T = TintBlock(curBlock, col1_0T);
                col1_1T = TintBlock(curBlock, col1_1T);
                col0_1T = TintBlock(curBlock, col0_1T);
            }
            
			
			// Draw Z axis
			int index = part.sIndex;
			part.vertices[index + 0] = new VertexP3fT2fC4b(X + 2.50f/16, Y, Z + 2.5f/16, u2, v2, col0_0);
			part.vertices[index + 1] = new VertexP3fT2fC4b(X + 2.50f/16, Y + blockHeight, Z + 2.5f/16, u2, v1, col0_0T);
			part.vertices[index + 2] = new VertexP3fT2fC4b(X + 13.5f/16, Y + blockHeight, Z + 13.5f/16, u1, v1, col1_1T);
			part.vertices[index + 3] = new VertexP3fT2fC4b(X + 13.5f/16, Y, Z + 13.5f/16, u1, v2, col1_1);
			
			// Draw Z axis mirrored
			index += part.sAdvance;
			part.vertices[index + 0] = new VertexP3fT2fC4b(X + 13.5f/16, Y, Z + 13.5f/16, u2, v2, col1_1);
			part.vertices[index + 1] = new VertexP3fT2fC4b(X + 13.5f/16, Y + blockHeight, Z + 13.5f/16, u2, v1, col1_1T);
			part.vertices[index + 2] = new VertexP3fT2fC4b(X + 2.50f/16, Y + blockHeight, Z + 2.5f/16, u1, v1, col0_0T);
			part.vertices[index + 3] = new VertexP3fT2fC4b(X + 2.50f/16, Y, Z + 2.5f/16, u1, v2, col0_0);
            
			// Draw X axis
			index += part.sAdvance;
			part.vertices[index + 0] = new VertexP3fT2fC4b(X + 2.50f/16, Y, Z + 13.5f/16, u2, v2, col0_1);
			part.vertices[index + 1] = new VertexP3fT2fC4b(X + 2.50f/16, Y + blockHeight, Z + 13.5f/16, u2, v1, col0_1T);
			part.vertices[index + 2] = new VertexP3fT2fC4b(X + 13.5f/16, Y + blockHeight, Z + 2.5f/16, u1, v1, col1_0T);
			part.vertices[index + 3] = new VertexP3fT2fC4b(X + 13.5f/16, Y, Z + 2.5f/16, u1, v2, col1_0);
			
			// Draw X axis mirrored
			index += part.sAdvance;
			part.vertices[index + 0] = new VertexP3fT2fC4b(X + 13.5f/16, Y, Z + 2.5f/16, u2, v2, col1_0);
			part.vertices[index + 1] = new VertexP3fT2fC4b(X + 13.5f/16, Y + blockHeight, Z + 2.5f/16, u2, v1, col1_0T);
			part.vertices[index + 2] = new VertexP3fT2fC4b(X + 2.50f/16, Y + blockHeight, Z + 13.5f/16, u1, v1, col0_1T);
			part.vertices[index + 3] = new VertexP3fT2fC4b(X + 2.50f/16, Y, Z + 13.5f/16, u1, v2, col0_1);
			
			part.sIndex += 4;
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
           
            #if !USE_DX
            return 255 << 24 | B << 16 | G << 8 | R;
            #else
            return 255 << 24 | R << 16 | G << 8 | B;
            #endif
        }
       
        FastColour GetBlockColorTop(int X, int Y, int Z, out bool blocksLight) {
            blocksLight = false;
            if (map.IsValidPos(X, Y, Z)) {
                BlockID thisBlock = map.GetBlock(X, Y, Z);
                if (isOccluder[thisBlock]) blocksLight = true;
               
                FastColour col = blocksLight ? dark : FastColour.Unpack(light.LightCol(X, Y, Z));
                if (BlockInfo.FullBright[thisBlock]) col = sun;
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
           
            #if !USE_DX
            return 255 << 24 | B << 16 | G << 8 | R;
            #else
            return 255 << 24 | R << 16 | G << 8 | B;
            #endif
        }
       
        FastColour GetBlockColorBottom(int X, int Y, int Z, out bool blocksLight) {
            blocksLight = false;
            if (map.IsValidPos(X, Y, Z)) {
                BlockID thisBlock = map.GetBlock(X, Y, Z);
                if (isOccluder[thisBlock]) blocksLight = true;
               
                FastColour col = blocksLight ? darkYBottom : FastColour.Unpack(light.LightCol_YBottom_Fast(X, Y, Z));
                if (BlockInfo.FullBright[thisBlock]) col = sun;
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
           
            #if !USE_DX
            return 255 << 24 | B << 16 | G << 8 | R;
            #else
            return 255 << 24 | R << 16 | G << 8 | B;
            #endif
        }
       
        FastColour GetBlockColorZSide(int X, int Y, int Z, out bool blocksLight) {
            blocksLight = false;
            if (map.IsValidPos(X, Y, Z)) {
                BlockID thisBlock = map.GetBlock(X, Y, Z);
                if (isOccluder[thisBlock]) blocksLight = true;
               
                FastColour col = blocksLight ? darkZ : FastColour.Unpack(light.LightCol_ZSide_Fast(X, Y, Z));
                if (BlockInfo.FullBright[thisBlock]) col = sun;
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
           
            #if !USE_DX
            return 255 << 24 | B << 16 | G << 8 | R;
            #else
            return 255 << 24 | R << 16 | G << 8 | B;
            #endif
        }
       
        FastColour GetBlockColorXSide(int X, int Y, int Z, out bool blocksLight) {
            blocksLight = false;
            if (map.IsValidPos(X, Y, Z)) {
                BlockID thisBlock = map.GetBlock(X, Y, Z);
                if (isOccluder[thisBlock]) blocksLight = true;
               
                FastColour col = blocksLight ? darkX : FastColour.Unpack(light.LightCol_XSide_Fast(X, Y, Z));
                if (BlockInfo.FullBright[thisBlock]) col = sun;
                return col;
            }
            return sunX;
        }
       
       
        #region Light computation
       
        int ComputeLightFlags(int x, int y, int z, int cIndex) {
            if (fullBright) return (1 << xP1_yP1_zP1) - 1; // all faces fully bright
 
            return
                Lit(x - 1, y, z - 1, cIndex - 1 - 18) << xM1_yM1_zM1 |
                Lit(x - 1, y, z,     cIndex - 1)      << xM1_yM1_zCC |
                Lit(x - 1, y, z + 1, cIndex - 1 + 18) << xM1_yM1_zP1 |
                Lit(x, y, z - 1,     cIndex + 0 - 18) << xCC_yM1_zM1 |
                Lit(x, y, z,         cIndex + 0)      << xCC_yM1_zCC |
                Lit(x, y, z + 1 ,    cIndex + 0 + 18) << xCC_yM1_zP1 |
                Lit(x + 1, y, z - 1, cIndex + 1 - 18) << xP1_yM1_zM1 |
                Lit(x + 1, y, z,     cIndex + 1)      << xP1_yM1_zCC |
                Lit(x + 1, y, z + 1, cIndex + 1 + 18) << xP1_yM1_zP1 ;
        }
       
        const int xM1_yM1_zM1 = 0,  xM1_yCC_zM1 = 1,  xM1_yP1_zM1 = 2;
        const int xCC_yM1_zM1 = 3,  xCC_yCC_zM1 = 4,  xCC_yP1_zM1 = 5;
        const int xP1_yM1_zM1 = 6,  xP1_yCC_zM1 = 7,  xP1_yP1_zM1 = 8;
 
        const int xM1_yM1_zCC = 9,  xM1_yCC_zCC = 10, xM1_yP1_zCC = 11;
        const int xCC_yM1_zCC = 12, xCC_yCC_zCC = 13, xCC_yP1_zCC = 14;
        const int xP1_yM1_zCC = 15, xP1_yCC_zCC = 16, xP1_yP1_zCC = 17;
 
        const int xM1_yM1_zP1 = 18, xM1_yCC_zP1 = 19, xM1_yP1_zP1 = 20;
        const int xCC_yM1_zP1 = 21, xCC_yCC_zP1 = 22, xCC_yP1_zP1 = 23;
        const int xP1_yM1_zP1 = 24, xP1_yCC_zP1 = 25, xP1_yP1_zP1 = 26;
       
        int Lit(int x, int y, int z, int cIndex) {
            if (x < 0 || y < 0 || z < 0
                || x >= width || y >= height || z >= length) return 7;
            int flags = 0;
            BlockID block = chunk[cIndex];
            int lightHeight = light.heightmap[(z * width) + x];
            lightFlags = BlockInfo.LightOffset[block];
 
            // Use fact Light(Y.Bottom) == Light((Y - 1).Top)
            int offset = (lightFlags >> Side.Bottom) & 1;
            flags |= ((y - offset) > lightHeight ? 1 : 0);
            // Light is same for all the horizontal faces
            flags |= (y > lightHeight ? 2 : 0);
            // Use fact Light((Y + 1).Bottom) == Light(Y.Top)
            offset = (lightFlags >> Side.Top) & 1;
            flags |= ((y - offset) >= lightHeight ? 4 : 0);
           
            // Dynamic lighting
            if (BlockInfo.FullBright[block])               flags |= 5;
            if (BlockInfo.FullBright[chunk[cIndex + 324]]) flags |= 4;
            if (BlockInfo.FullBright[chunk[cIndex - 324]]) flags |= 1;
            return flags;
        }
       
        static int[] masks = {
            // Left face
            (1 << xM1_yM1_zM1) | (1 << xM1_yM1_zCC) | (1 << xM1_yM1_zP1) |
                (1 << xM1_yCC_zM1) | (1 << xM1_yCC_zCC) | (1 << xM1_yCC_zP1) |
                (1 << xM1_yP1_zM1) | (1 << xM1_yP1_zCC) | (1 << xM1_yP1_zP1),
            // Right face
            (1 << xP1_yM1_zM1) | (1 << xP1_yM1_zCC) | (1 << xP1_yM1_zP1) |
                (1 << xP1_yP1_zM1) | (1 << xP1_yP1_zCC) | (1 << xP1_yP1_zP1) |
                (1 << xP1_yCC_zM1) | (1 << xP1_yCC_zCC) | (1 << xP1_yCC_zP1),
            // Front face
            (1 << xM1_yM1_zM1) | (1 << xCC_yM1_zM1) | (1 << xP1_yM1_zM1) |
                (1 << xM1_yCC_zM1) | (1 << xCC_yCC_zM1) | (1 << xP1_yCC_zM1) |
                (1 << xM1_yP1_zM1) | (1 << xCC_yP1_zM1) | (1 << xP1_yP1_zM1),
            // Back face
            (1 << xM1_yM1_zP1) | (1 << xCC_yM1_zP1) | (1 << xP1_yM1_zP1) |
                (1 << xM1_yCC_zP1) | (1 << xCC_yCC_zP1) | (1 << xP1_yCC_zP1) |
                (1 << xM1_yP1_zP1) | (1 << xCC_yP1_zP1) | (1 << xP1_yP1_zP1),
            // Bottom face
            (1 << xM1_yM1_zM1) | (1 << xM1_yM1_zCC) | (1 << xM1_yM1_zP1) |
                (1 << xCC_yM1_zM1) | (1 << xCC_yM1_zCC) | (1 << xCC_yM1_zP1) |
                (1 << xP1_yM1_zM1) | (1 << xP1_yM1_zCC) | (1 << xP1_yM1_zP1),
            // Top face
            (1 << xM1_yP1_zM1) | (1 << xM1_yP1_zCC) | (1 << xM1_yP1_zP1) |
                (1 << xCC_yP1_zM1) | (1 << xCC_yP1_zCC) | (1 << xCC_yP1_zP1) |
                (1 << xP1_yP1_zM1) | (1 << xP1_yP1_zCC) | (1 << xP1_yP1_zP1),
        };
       
        #endregion
    }
}