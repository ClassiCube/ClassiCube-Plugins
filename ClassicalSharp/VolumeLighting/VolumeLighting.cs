﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.Events;
using ClassicalSharp.Map;

namespace VolumeLightingPlugin {
	
	/// <summary> Manages lighting through a simple heightmap, where each block is either in sun or shadow. </summary>
	public sealed partial class VolumeLighting : IWorldLighting {

		Game game;
		public const byte lightExtent = 16, maxLight = (byte)(lightExtent - 1);
		byte[, ,] lightLevels;
		int[] lightmap, lightmapZSide, lightmapXSide, lightmapYBottom;
		
		public override void Reset(Game game) { heightmap = null; }
		
		public override void OnNewMap(Game game) {
			UpdateLightmap();
		}
		
		public override void OnNewMapLoaded(Game game) {
			width = game.World.Width;
			height = game.World.Height;
			length = game.World.Length;
			this.game = game;
			
			lightLevels = new byte[width, height, length];
			CastInitial(0, 0, width, length);			
			for( int pass = maxLight; pass > 1; pass-- ) {
				Console.WriteLine("Starting pass " + pass + "." );
				DoPass(pass, 0, 0, 0, width, height, length);
			}
		}
		
		public override void Init(Game game) {
			this.game = game;
			game.WorldEvents.EnvVariableChanged += EnvVariableChanged;
			lightmap = new int[lightExtent * lightExtent];
			lightmapXSide = new int[lightExtent * lightExtent];
			lightmapZSide = new int[lightExtent * lightExtent];
			lightmapYBottom = new int[lightExtent * lightExtent];
		}
		
		public override void Dispose() {
			game.WorldEvents.EnvVariableChanged -= EnvVariableChanged;
			heightmap = null;
		}

		void EnvVariableChanged(object sender, EnvVarEventArgs e) {
			if (e.Var == EnvVar.ShadowlightColour || e.Var == EnvVar.SunlightColour) {
				UpdateLightmap();
				game.MapRenderer.Refresh();
			}
		}
		
		void UpdateLightmap() {
			FastColour sun = game.World.Env.Sunlight, shadow = game.World.Env.Shadowlight;
			
			FastColour darkShadow = shadow;
			FastColour torchLight = new FastColour(249, 218, 185, 255);
			darkShadow.R >>= 3; darkShadow.G >>= 3; darkShadow.B >>= 3;
			
			FastColour halfSun = FastColour.Lerp(shadow, sun, 0.5f);
			FastColour torchSun = new FastColour(Math.Max(torchLight.R, sun.R), Math.Max(torchLight.G, sun.G), Math.Max(torchLight.B, sun.B));
			for (int y = 0; y < lightExtent; y++) {
				
				float lerpY = y / (float)(lightExtent - 1);
				lerpY = 1.0f - (float)Math.Cos(lerpY *Math.PI * 0.5f);
				
				FastColour lerpShadow = FastColour.Lerp(darkShadow, torchLight, lerpY);
				FastColour lerpHalfLight = FastColour.Lerp(halfSun, torchLight, lerpY);
				FastColour lerpLight = FastColour.Lerp(sun, torchLight, lerpY);
				
				for (int x = 0; x < lightExtent; x++)
				{
					//1 -cos
					float lerpX = x / (float)(lightExtent - 1);
					lerpX = 1.0f -(float)Math.Cos(lerpX *Math.PI * 0.5f);
					FastColour col = FastColour.Lerp(lerpShadow, lerpHalfLight, lerpX);
					SetLightmap(x, y, col);
				}
				SetLightmap(15, y, lerpLight);
			}
			
			Outside = lightmap[maxLight * lightExtent];
			OutsideXSide = lightmapXSide[maxLight * lightExtent];
			OutsideZSide = lightmapZSide[maxLight * lightExtent];
			OutsideYBottom = lightmapYBottom[maxLight * lightExtent];
		}

		void TextureChanged(object sender, TextureEventArgs e) {
			return; //because I'm trying to test using only the env colors
			if (e.Name != "lightmap.png") return;
			
			using (MemoryStream ms = new MemoryStream(e.Data))
				using (Bitmap bmp = Platform.ReadBmp(ms))
			{
				if (bmp.Width != lightExtent || bmp.Height != lightExtent) {
					game.Chat.Add("&clightmap.png must be " + lightExtent + "x" + lightExtent + "."); return;
				}
				
				// Not bothering with FastBitmap here as perf increase is insignificant.
				for (int y = 0; y < lightExtent; y++)
					for (int x = 0; x < lightExtent; x++)
				{
					Color col = bmp.GetPixel(x, y);
					SetLightmap(x, y, new FastColour(col));
				}
			}
			game.MapRenderer.Refresh();
		}
		
		void SetLightmap(int x, int y, FastColour col) {
			lightmap[x * 16 + y] = col.Pack();
			FastColour.GetShaded(col, out lightmapXSide[x * 16 + y],
			                     out lightmapZSide[x * 16 + y], out lightmapYBottom[x * 16 + y]);
		}
		
		
		public unsafe override void LightHint(int startX, int startZ, byte* mapPtr) {
		}
		
		// Outside colour is same as sunlight colour, so we reuse when possible
		public override bool IsLit(int x, int y, int z) {
			int light = lightLevels[x, y, z];
			light = Math.Max(light >> 4, light & 0xF);
			return light >= 7;
		}

		public override int LightCol(int x, int y, int z) {
			return lightmap[lightLevels[x, y, z]];
		}
		
		public override int LightCol_ZSide(int x, int y, int z) {
			return lightmapZSide[lightLevels[x, y, z]];
		}
		

		public override int LightCol_Sprite_Fast(int x, int y, int z) {
			return lightmap[lightLevels[x, y, z]];
		}
		
		public override int LightCol_YTop_Fast(int x, int y, int z) {
			y += 1;
			if (y >= height) return Outside;
			return lightmap[lightLevels[x, y, z]];
		}
		
		public override int LightCol_YBottom_Fast(int x, int y, int z) {
			return lightmapYBottom[lightLevels[x, y, z]];
		}
		
		public override int LightCol_XSide_Fast(int x, int y, int z) {
			return lightmapXSide[lightLevels[x, y, z]];
		}
		
		public override int LightCol_ZSide_Fast(int x, int y, int z) {
			return lightmapZSide[lightLevels[x, y, z]];
		}
		
		
		public override void OnBlockChanged(int x, int y, int z, byte oldBlock, byte newBlock) {
			x &= ~0x0F; y &= ~0x0F; z &= ~0x0F;
			int minX = Math.Max(x - 16, 0), minY = Math.Max(y - 16, 0), minZ = Math.Max(z - 16, 0);
			int maxX = Math.Min(x + 16, width), maxY = Math.Min(y + 16, height), maxZ = Math.Min(z + 16, length);
			
			for (int yy = minY; yy < maxY; yy++)
				for (int zz = minZ; zz < maxZ; zz++)
					for (int xx = minX; xx < maxX; xx++)
			{
				lightLevels[xx, yy, zz] = 0;
			}
			
			CastInitial(minX, minZ, maxX, maxZ);
			for( int pass = maxLight; pass > 1; pass-- ) {
				DoPass(pass, minX, minY, minZ, maxX, maxY, maxZ);
			}
			
			x >>= 4; y >>= 4; z >>= 4;
			for (int yy = y - 1; yy <= y + 1; yy++) {
				game.MapRenderer.RefreshChunk(x - 1, yy, z - 1);
				game.MapRenderer.RefreshChunk(x + 0, yy, z - 1);
				game.MapRenderer.RefreshChunk(x + 1, yy, z - 1);
				
				game.MapRenderer.RefreshChunk(x - 1, yy, z);
				game.MapRenderer.RefreshChunk(x + 0, yy, z);
				game.MapRenderer.RefreshChunk(x + 1, yy, z);
				
				game.MapRenderer.RefreshChunk(x - 1, yy, z + 1);
				game.MapRenderer.RefreshChunk(x + 0, yy, z + 1);
				game.MapRenderer.RefreshChunk(x + 1, yy, z + 1);
			}
		}
	}
}