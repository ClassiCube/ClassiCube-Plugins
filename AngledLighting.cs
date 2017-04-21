using System;
using ClassicalSharp.Events;

#if USE16_BIT
using BlockID = System.UInt16;
#else
using BlockID = System.Byte;
#endif

namespace ClassicalSharp.Map {

	public sealed partial class FailLighting : IWorldLighting {
		
		int oneY, shadow, shadowZSide, shadowXSide, shadowYBottom;
		BlockInfo info;
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
			info = game.BlockInfo;
			this.game = game;
			oneY = width * length;
			
			heightmap = new short[width * length];
			
			blockers = new int[(width + height) * (length + height)];
			CalcLightDepths(0, 0, width, length);
		}
		
		const int dx = -1, dz = -1;
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
			
			for (int x = xStart; x < xStart + xWidth; ++x) { //from 0 to the width + height of the map
				for (int z = zStart; z < zStart + zLength; ++z) { //from 0 to the length of the map
					
					int oldY = blockers[x + z * xExtent];
					
					int y = height -1; //-1 because it starts at 0
					int xD = x + height -1;
					int zD = z + height -1;
					
					int xOver = 0; //how far past the edge of the map is it?
					int zOver = 0;
					

					if (xD >= xExtent) {
						xOver = xD - (xExtent -1);
					}
					if (zD >= zExtent) {
						zOver = zD - (zExtent -1);//how far past the edge of the map is it?
					}
					int maxOver = Math.Max(xOver, zOver);
					//pushing y and x and z back to the edge of the map
					y -= maxOver;
					xD -= maxOver;
					zD -= maxOver;
					
					xD -= height;
					zD -= height;
					
					while (y > 0 && xD >= 0 && xD < width && zD >= 0 && zD < length &&
					       !info.BlocksLight[map.GetBlock(xD, y, zD)]) {
						xD += dx; y -= 1; zD += dz;
					}
					
					if (xD < 0 || zD < 0) {
						y = oldY;
					}
					blockers[x + z * xExtent] = y;
					
				}
			}
		}
		
		public override void Init(Game game) {
			game.WorldEvents.EnvVariableChanged += EnvVariableChanged;
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
		}
		
		public override void Dispose() {
			if (game != null)
				game.WorldEvents.EnvVariableChanged -= EnvVariableChanged;
			heightmap = null;
		}

		void EnvVariableChanged(object sender, EnvVarEventArgs e) {
			if (e.Var == EnvVar.SunlightColour) {
				SetSun(game.World.Env.Sunlight);
			} else if (e.Var == EnvVar.ShadowlightColour) {
				SetShadow(game.World.Env.Shadowlight);
			}
		}
		
		void SetSun(FastColour col) {
			Outside = col.Pack();
			FastColour.GetShaded(col, out OutsideXSide, out OutsideZSide, out OutsideYBottom);
		}
		
		void SetShadow(FastColour col) {
			shadow = col.Pack();
			FastColour.GetShaded(col, out shadowXSide, out shadowZSide, out shadowYBottom);
		}
		
		
		public unsafe override void LightHint(int startX, int startZ, BlockID* mapPtr) {
		}
		
		public override int GetLightHeight(int x, int z) {
			int index = (z * width) + x;
			return heightmap[index];
		}
		
		
		// Outside colour is same as sunlight colour, so we reuse when possible
		public override bool IsLit(int x, int y, int z) {
			return !(x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < length)
				|| y >= blockers[(x +height -y) + (z +height -y) * (width + height)];
		}

		public override int LightCol(int x, int y, int z) {
			//return y > GetLightHeight(x, z) ? Outside : shadow;
			return IsLit(x, y + 1, z) ? Outside : shadowZSide;
		}
		
		public override int LightCol_ZSide(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideZSide : shadowXSide;
		}		

		public override int LightCol_Sprite_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? Outside : shadowXSide;
		}
		
		public override int LightCol_YTop_Fast(int x, int y, int z) {
			return IsLit(x, y + 1, z) ? Outside : shadowZSide;
		}
		
		public override int LightCol_YBottom_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideYBottom : shadowYBottom;
		}
		
		public override int LightCol_XSide_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideXSide : shadowXSide;
		}
		
		public override int LightCol_ZSide_Fast(int x, int y, int z) {
			return IsLit(x, y, z) ? OutsideZSide : shadowXSide;
		}
		
		
		public override void UpdateLight(int x, int y, int z, BlockID oldBlock, BlockID newBlock) {
		}
	}
}
