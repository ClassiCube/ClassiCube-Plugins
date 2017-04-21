// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
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
		
		public override void Reset(Game game) { heightmap = null; }
		
		public override void OnNewMap(Game game) {
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
			heightmap = null;
		}
		
		public override void OnNewMapLoaded(Game game) {
			width = game.World.Width;
			height = game.World.Height;
			length = game.World.Length;
			info = game.BlockInfo;
			this.game = game;
			oneY = width * length;
			
			heightmap = new short[width * length];
			CalcLightDepths();
		}
		
		void CalcLightDepths() {
			int i = 0;
			World map = game.World;
			for (int z = 0; z < length; z++)
				for (int x = 0; x < width; x++)
			{
				for (int y = height - 1; y >= heightmap[i]; y--) {
					if (info.BlocksLight[map.GetBlock(x, y, z)]) {
						CastShadow(x, y - 1, z);
						break;
					}
				}				
				i++;
			}
		}
		
		void CastShadow(int x, int y, int z) {
			const int dirX = -1, dirZ = 1;
			
			while (x >= 0 && z >= 0 && x < width && z < length) {
				int hIndex = z * width + x;
				heightmap[hIndex] = (short)Math.Max(y, heightmap[hIndex]);
				
				y--;
				x += dirX; z += dirZ;
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
			return y > GetLightHeight(x, z);
		}

		public override int LightCol(int x, int y, int z) {
			return y > GetLightHeight(x, z) ? Outside : shadow;
		}
		
		public override int LightCol_ZSide(int x, int y, int z) {
			return y > GetLightHeight(x, z) ? OutsideZSide : shadowZSide;
		}
		

		public override int LightCol_Sprite_Fast(int x, int y, int z) {
			return y > heightmap[(z * width) + x] ? Outside : shadow;
		}
		
		public override int LightCol_YTop_Fast(int x, int y, int z) {
			return y >= heightmap[(z * width) + x] ? Outside : shadow;
		}
		
		public override int LightCol_YBottom_Fast(int x, int y, int z) {
			return y > heightmap[(z * width) + x] ? OutsideYBottom : shadowYBottom;
		}
		
		public override int LightCol_XSide_Fast(int x, int y, int z) {
			return y > heightmap[(z * width) + x] ? OutsideXSide : shadowXSide;
		}
		
		public override int LightCol_ZSide_Fast(int x, int y, int z) {
			return y > heightmap[(z * width) + x] ? OutsideZSide : shadowZSide;
		}
		
		
		public override void UpdateLight(int x, int y, int z, BlockID oldBlock, BlockID newBlock) {
		}
	}
}
