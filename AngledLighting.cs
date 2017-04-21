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
			blockers = new int[(width + height) * length];
			CalcLightDepths(0, 0, width, length);
		}
		
		
        public void CalcLightDepths(int xStart, int zStart, int xWidth, int zDepth) { //flag1
            
        	World map = game.World;
        	
        	xStart += height;
        	if (xWidth == width) {
        		xWidth += height;
        		xStart -= height;
        	}
        	int widthRow = width + height;
            for (int x = xStart; x < xStart + xWidth; ++x) {
                for (int z = zStart; z < zStart + zDepth; ++z) {
                    int oldY = blockers[x + z * widthRow];
     
                    int y = height -1;
                    int xD = x + height -1;
                    if (xD >= widthRow) {
                    	int xOver = xD - (widthRow -1);
                    	y -= xOver;
                    	xD -= xOver;
                    }
                    xD -= height;
                    while (y > 0 && xD >= 0 && xD < width && !info.BlocksLight[map.GetBlock(xD, y, z)]) {
                        --y;
                        --xD;
                    }
                    if (xD < 0) {
                    	y = oldY;
                    }
                    blockers[x + z * widthRow] = y; //blockers becomes y
                }
            }
        }
		
		/*void CalcLightDepths() {
			int i = 0;
			World map = game.World;
			for (int z = 0; z < length; z++)
				for (int x = 0; x < width; x++)
			{
				for (int y = height - 1; y >= heightmap[i]; y--) {
					if (info.BlocksLight[map.GetBlock(x, y, z)]) {
						CastShadow(x, y, z);
						break;
					}
				}				
				i++;
			}
		}
		
		void CastShadow(int x, int y, int z) {
			const int dirX = 1, dirZ = 1;
			
			while (x >= 0 && z >= 0 && x < width && z < length) {
				int hIndex = z * width + x;
				heightmap[hIndex] = (short)Math.Max(y, heightmap[hIndex]);
				
				y--;
				x += dirX; z += dirZ;
			}
		}*/
		
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
            return !(x >= 0 && y >= 0 && z >= 0 && x < width && y < height
            && z < length) || y >= blockers[(x +height -y) + z * (width + height)]; //flag2
		}

		public override int LightCol(int x, int y, int z) {
			//return y > GetLightHeight(x, z) ? Outside : shadow;
			if (IsLit(x, y + 1, z)) {
			    return Outside;
			}
			return shadowZSide;
		}
		
		public override int LightCol_ZSide(int x, int y, int z) {
			if (IsLit(x, y, z)) {
			    return OutsideZSide;
			}
			return shadowXSide;
		}
		

		public override int LightCol_Sprite_Fast(int x, int y, int z) {
			if (IsLit(x, y, z)) {
			    return Outside;
			}
			return shadowXSide;
		}
		
		public override int LightCol_YTop_Fast(int x, int y, int z) {
			if (IsLit(x, y + 1, z)) {
			    return Outside;
			}
			return shadowXSide;
		}
		
		public override int LightCol_YBottom_Fast(int x, int y, int z) {
			if (IsLit(x, y, z)) {
			    return OutsideYBottom;
			}
			return shadowYBottom;
		}
		
		public override int LightCol_XSide_Fast(int x, int y, int z) {
			if (IsLit(x, y, z)) {
			    return OutsideXSide;
			}
			return shadowXSide;
		}
		
		public override int LightCol_ZSide_Fast(int x, int y, int z) {
			if (IsLit(x, y, z)) {
			    return OutsideZSide;
			}
			return shadowXSide;
		}
		
		
		public override void UpdateLight(int x, int y, int z, BlockID oldBlock, BlockID newBlock) {
		}
	}
}
