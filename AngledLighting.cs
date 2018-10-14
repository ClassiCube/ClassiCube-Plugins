// Idea and original code designed by goodly's dad

using System;
using ClassicalSharp;
using ClassicalSharp.Map;
using BlockID = System.UInt16;

namespace AngledLightingPlugin {

	public sealed class Core : Plugin {
		
		public int APIVersion { get { return 2; } }
		
		public void Dispose() { }
		
		public void Init(Game game) {
			game.Lighting.Dispose();
			game.Components.Remove(game.Lighting);
			
			game.Lighting = new AngledLighting();
			game.Lighting.Init(game);
			game.Components.Add(game.Lighting);
		}
		
		public void Ready(Game game) { }
		
		public void Reset(Game game) { }
		
		public void OnNewMap(Game game) { }
		
		public void OnNewMapLoaded(Game game) { }
	}

	public sealed class AngledLighting : IWorldLighting {
		
		int oneY;
		PackedCol shadow, shadowZSide, shadowXSide, shadowYBottom;
		Game game;
		int[] blockers;
		public override void Reset(Game game) { heightmap = null; blockers = null; }
		
		public override void OnNewMap(Game game) {
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
			heightmap = null;
			blockers = null;
		}
		
		public override void OnNewMapLoaded(Game game) {
			width = game.World.Width;
			height = game.World.Height;
			length = game.World.Length;
			this.game = game;
			oneY = width * length;
			
			heightmap = new short[width * length];
			
			blockers = new int[(width + height) * (length + height)];
			CalcLightDepths(0, 0, width, length);
		}
		
		const float ShadeASX = 0.8f, ShadeASZ = 0.9f, ShadeASYBottom = 0.7f;
		static void GetShadedAngleSun(PackedCol normal, out PackedCol xSide, out PackedCol zSide, out PackedCol yBottom) {
			xSide   = PackedCol.Scale(normal, ShadeASX);
			zSide   = PackedCol.Scale(normal, ShadeASZ);
			yBottom = PackedCol.Scale(normal, ShadeASYBottom);
		}
		
		const float ShadeAHX = 0.6f, ShadeAHZ = 0.8f, ShadeAHYBottom = 0.5f;
		static void GetShadedAngleShadow(PackedCol normal, out PackedCol xSide, out PackedCol zSide, out PackedCol yBottom) {
			xSide   = PackedCol.Scale(normal, ShadeAHX);
			zSide   = PackedCol.Scale(normal, ShadeAHZ);
			yBottom = PackedCol.Scale(normal, ShadeAHYBottom);
		}
		
		
		public void CalcLightDepths(int xStart, int zStart, int xWidth, int zLength) {
			//xStart and zStart are zero.
			World map = game.World;
			
			xStart += height; //add xStart to the height of the map because
			if (xWidth == width) { //Since xWidth starts the same as width, this always happens at first
				xWidth += height;
				xStart -= height;
				//xStart is zero again...
				//xWidth is now equal to the height of the map
			}
			
			zStart += height;
			if (zLength == length) {
				zLength += height;
				zStart -= height;
			}
			
			//the size of the lightmap in each dimension
			int xExtent = width + height;
			int zExtent = length + height;
			
			bool[] blocksLight = BlockInfo.BlocksLight;
			
			for (int x = xStart; x < xStart + xWidth; ++x) { //from 0 to the width + height of the map
				for (int z = zStart; z < zStart + zLength; ++z) { //from 0 to the length of the map
					
					int oldY = blockers[x + z * xExtent];
					
					int yCur = height -1; //-1 because it starts at 0
					int xCur = x + height -1;
					int zCur = z + height -1;
					
					int xOver = 0; //how far past the edge of the map is it?
					int zOver = 0;
					

					if (xCur >= xExtent) {
						xOver = xCur - (xExtent -1);
					}
					if (zCur >= zExtent) {
						zOver = zCur - (zExtent -1);//how far past the edge of the map is it?
					}
					int maxOver = Math.Max(xOver, zOver);
					//pushing y and x and z back to the edge of the map
					yCur -= maxOver;
					xCur -= maxOver;
					zCur -= maxOver;
					
					xCur -= height;
					zCur -= height;
					
					int yNext = (yCur > 0) ? yCur - 1 : yCur;					
					int xNext = (xCur > 0) ? xCur - 1 : xCur;
					int zNext = (zCur > 0) ? zCur - 1 : zCur;
					
					while (yCur > 0 && xCur >= 0 && xCur < width && zCur >= 0 && zCur < length &&
					       !(blocksLight[map.GetBlock(xCur,  yCur,  zCur)] || blocksLight[map.GetBlock(xCur, yNext, zCur)])  &&					       
					       !(blocksLight[map.GetBlock(xNext, yCur,  zCur)] || blocksLight[map.GetBlock(xCur, yCur,  zNext)]) &&
					       !(blocksLight[map.GetBlock(xNext, yNext, zCur)] || blocksLight[map.GetBlock(xCur, yNext, zNext)])
					      ) {
						
						--yCur; --xCur; --zCur;						
						yNext = (yCur > 0) ? yCur - 1 : yCur;
						xNext = (xCur > 0) ? xCur - 1 : xCur;
						zNext = (zCur > 0) ? zCur - 1 : zCur;
					}
					
					if (xCur < 0 || zCur < 0) {
						yCur = oldY;
					}
					blockers[x + z * xExtent] = yCur;
					
				}
			}
		}
		
		public override void Init(Game game) {
			Events.EnvVariableChanged += EnvVariableChanged;
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
		}
		
		public override void Dispose() {
			if (game != null)
				Events.EnvVariableChanged -= EnvVariableChanged;
			heightmap = null;
		}

		void EnvVariableChanged(EnvVar envVar) {
			if (envVar == EnvVar.SunCol) {
				SetSun(game.World.Env.Sun);
			} else if (envVar == EnvVar.ShadowCol) {
				SetShadow(game.World.Env.Shadow);
			}
		}
		
		void SetSun(PackedCol col) {
			Outside = col;
			GetShadedAngleSun(col, out OutsideXSide, out OutsideZSide, out OutsideYBottom);
		}
		
		void SetShadow(PackedCol col) {
			shadow = col;
			GetShadedAngleShadow(col, out shadowXSide, out shadowZSide, out shadowYBottom);
		}
		
		
		public unsafe override void LightHint(int startX, int startZ, byte* mapPtr) {
		}
		
		
		// Outside colour is same as sunlight colour, so we reuse when possible
		public override bool IsLit(int x, int y, int z) {
			return !(x >= 0 &&
			         y >= 0 &&
			         z >= 0 &&
			         x < width &&
			         y < height &&
			         z < length) || y >= blockers[(x +height -y) + (z +height -y) * (width + height)];
		}

		public override PackedCol LightCol(int x, int y, int z) {
			//return y > GetLightHeight(x, z) ? Outside : shadow;
			return IsLit(x, y, z) ? Outside : shadow;
		}
		
		public override PackedCol LightCol_ZSide(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideZSide : shadowZSide;
		}		

		public override PackedCol LightCol_Sprite_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? Outside : shadow;
		}
		
		public override PackedCol LightCol_YTop_Fast(int x, int y, int z) {
			return IsLit(x, y + 1, z) ? Outside : shadow;
		}
		
		public override PackedCol LightCol_YBottom_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideYBottom : shadowYBottom;
		}
		
		public override PackedCol LightCol_XSide_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideXSide : shadowXSide;
		}
		
		public override PackedCol LightCol_ZSide_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideZSide : shadowZSide;
		}
		
		
		public override void Refresh() {
			for (int i = 0; i < heightmap.Length; i++)
				heightmap[i] = short.MaxValue;
		}
		
		public override void OnBlockChanged(int x, int y, int z, BlockID oldBlock, BlockID newBlock) {
			if (!BlockInfo.BlocksLight[newBlock] && !BlockInfo.BlocksLight[oldBlock]) return;		
			int cX = x >> 4, cY = y >> 4, cZ = z >> 4;
			
			int xWidth = 2;
			int zLength = 2;
			if (x + xWidth >= width) { xWidth--; }
			if (z + zLength >= length) { zLength--; }
			
			CalcLightDepths((x -1) - y, (z -1) - y, xWidth, zLength);
			CalcLightDepths((x) - y, (z) - y, xWidth, zLength);

			do {
				if (game.SmoothLighting) {
					int bX = x & 0xF, bY = y & 0xF, bZ = z & 0xF;				
					
					if (bX == 15) {
						game.MapRenderer.RefreshChunk(cX +1, cY, cZ);
					}
					if (bY == 15) {
						game.MapRenderer.RefreshChunk(cX, cY +1, cZ);
					}
					if (bZ == 15) {
						game.MapRenderer.RefreshChunk(cX, cY, cZ +1);
					}
					
					// corner chunks
					if (bX == 15 && bZ == 15) {
						game.MapRenderer.RefreshChunk(cX +1, cY, cZ +1);
					}
					if (bX == 15 && bY == 15 && bZ == 15) {
						game.MapRenderer.RefreshChunk(cX +1, cY +1, cZ +1);
					}
					
					if (bX == 15 && bZ == 0) {
						game.MapRenderer.RefreshChunk(cX +1, cY, cZ -1);
					}
					if (bZ == 15 && bX == 0) {
						game.MapRenderer.RefreshChunk(cX -1, cY, cZ +1);
					}
				}
				
				game.MapRenderer.RefreshChunk(cX, cY, cZ);
				if (cX > 0) {
					game.MapRenderer.RefreshChunk(cX -1, cY, cZ);
				}
				if (cZ > 0) {
					game.MapRenderer.RefreshChunk(cX, cY, cZ -1);
				}
				if (cX > 0 && cZ > 0) {
					game.MapRenderer.RefreshChunk(cX -1, cY, cZ -1);
				}
				
				if (y > 0) {
					game.MapRenderer.RefreshChunk(cX, cY -1, cZ);
					
					if (cX > 0) {
						game.MapRenderer.RefreshChunk(cX -1, cY -1, cZ);
					}
					if (cZ > 0) {
						game.MapRenderer.RefreshChunk(cX, cY -1, cZ -1);
					}
					if (cX > 0 && cZ > 0) {
						game.MapRenderer.RefreshChunk(cX -1, cY -1, cZ -1);
					}
				}

				cX--; cY--; cZ--;
			} while (cX >= 0 && cZ >= 0 && cY >= 0);			
		}
	}
}