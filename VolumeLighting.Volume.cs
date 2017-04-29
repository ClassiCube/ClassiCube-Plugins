// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using ClassicalSharp;
using ClassicalSharp.Events;
using ClassicalSharp.Map;

namespace VolumeLightingPlugin {
	
	/// <summary> Manages lighting through a simple heightmap, where each block is either in sun or shadow. </summary>
	public sealed partial class VolumeLighting : IWorldLighting {
		
		void CastInitial(int startX, int startZ, int endX, int endZ) {
			//initial loop for making fullbright spots
			int oneY = width * length, maxY = height - 1;
			byte[] blocks = game.World.blocks;
			
			FastQueue queue = new FastQueue(32 * 32 * 32);
			
			for( int z = startZ; z < endZ; z++ ) {
				int horOffset = startX + (z * width);
				for( int x = startX; x < endX; x++ ) {
					int index = (maxY * oneY) + horOffset;
					horOffset++; // increase horizontal position
					int lightHeight = CalcHeightAt(x, z);
					
					for( int y = maxY; y >= 0; y-- ) {
						byte curBlock = blocks[index];
						//if the current block is in sunlight assign the fullest sky brightness to the higher 4 bits
						if( (y - 1) > lightHeight ) { lightLevels[x, y, z] = (byte)(maxLight << 4); }
						
						//if the current block is fullbright assign the fullest block brightness to the higher 4 bits
						if( info.FullBright[curBlock] ) queue.Enqueue(index, 15);
						
						index -= oneY; // reduce y position
					}
				}
			}
			
			CastBlockLight(queue);
		}
		
		int CalcHeightAt(int x, int z) {
			int mapIndex = ((height - 1) * length + z) * width + x;
			byte[] blocks = game.World.blocks;
			
			for (int y = height - 1; y >= 0; y--) {
				byte block = blocks[mapIndex];
				if (info.BlocksLight[block]) {
					int offset = (info.LightOffset[block] >> Side.Top) & 1;
					return y - offset;
				}
				mapIndex -= width * length;
			}
			return -10;
		}

		// ================
		// === SKYLIGHT ===
		// ================
		void DoPass(int pass, int startX, int startY, int startZ, int endX, int endY, int endZ) {
			int index = 0;
			bool[] lightPasses = new bool[Block.Count];
			byte[] blocks = game.World.blocks;
			int maxX = width - 1, maxY = height - 1, maxZ = length - 1;
			
			for (int i = 0; i < lightPasses.Length; i++) {
				// Light passes through a block if a) doesn't block light b) block isn't full block
				lightPasses[i] =
					!game.BlockInfo.BlocksLight[i] ||
					game.BlockInfo.MinBB[i] != OpenTK.Vector3.Zero ||
					game.BlockInfo.MaxBB[i] != OpenTK.Vector3.One;
			}
			
			for( int y = startY; y < endY; y++ )
				for( int z = startZ; z < endZ; z++ )
					for( int x = startX; x < endX; x++ )
			{
				index = x + width * (z + length * y);
				byte curBlock = blocks[index];
				
				int skyLight = lightLevels[x, y, z] >> 4;
				//if the current block is not a light blocker AND the current spot is less than i
				if( !info.BlocksLight[curBlock] && skyLight == pass ) {
					//check the six neighbors sky light value,
					if( y < maxY && skyLight > (lightLevels[x, y+1, z] >> 4) ) {
						if( lightPasses[blocks[index + width * length]] ){
							lightLevels[x, y+1, z] &= 0x0F; // reset skylight bits to 0
							lightLevels[x, y+1, z] |= (byte)((skyLight - 1) << 4); // set skylight bits
						}
					}
					if( y > 0 && skyLight > (lightLevels[x, y-1, z] >> 4) ) {
						if( lightPasses[blocks[index - width * length]] ) {
							lightLevels[x, y-1, z] &= 0x0F;
							lightLevels[x, y-1, z] |= (byte)((skyLight - 1) << 4);
						}
					}
					if( x < maxX && skyLight > (lightLevels[x+1, y, z] >> 4) ) {
						if( lightPasses[blocks[index + 1]] ) {
							lightLevels[x+1, y, z] &= 0x0F;
							lightLevels[x+1, y, z] |= (byte)((skyLight - 1) << 4);
						}
					}
					if( x > 0 && skyLight > (lightLevels[x-1, y, z] >> 4) ) {
						if( lightPasses[blocks[index - 1]]) {
							lightLevels[x-1, y, z] &= 0x0F;
							lightLevels[x-1, y, z] |= (byte)((skyLight - 1) << 4);
						}
					}
					if( z < maxZ && skyLight > (lightLevels[x, y, z+1] >> 4) ) {
						if( lightPasses[blocks[index + width]]) {
							lightLevels[x, y, z+1] &= 0x0F;
							lightLevels[x, y, z+1] |= (byte)((skyLight - 1) << 4);
						}
					}
					if( z > 0 && skyLight > (lightLevels[x, y, z-1] >> 4) ) {
						if( lightPasses[blocks[index - width]]) {
							lightLevels[x, y, z-1] &= 0x0F;
							lightLevels[x, y, z-1] |= (byte)((skyLight - 1) << 4);
						}
					}
				}
				index++; // increase one coord
			}
		}
		
		// ===================
		// === BLOCK LIGHT ===
		// ===================		
		void CastBlockLight(FastQueue queue) {
			BlockInfo info = game.BlockInfo;
			byte[] blocks = game.World.blocks;
			int maxX = width - 1, maxY = height - 1, maxZ = length - 1;
			int oneY = width * length;
			
			while (queue.count > 0) {
				int idx;
				byte light;
				queue.Dequeue(out idx, out light);

				int x = idx % width;
				int y = idx / oneY;
				int z = (idx / width) % length;

				lightLevels[x, y, z] &= 0xF0;
				lightLevels[x, y, z] |= light;
				
				if (light == 1) break; // doesn't cast further
				light--;
				
				if (x > 0 && !info.BlocksLight[blocks[idx - 1]] && light > (lightLevels[x - 1, y, z] & 0x0f)) {
					queue.Enqueue(idx - 1, light);
				}
				if (x < maxX && !info.BlocksLight[blocks[idx + 1]] && light > (lightLevels[x + 1, y, z] & 0x0f)) {
					queue.Enqueue(idx + 1, light);
				}
				if (z > 0 && !info.BlocksLight[blocks[idx - width]] && light > (lightLevels[x, y, z - 1] & 0x0f)) {
					queue.Enqueue(idx - width, light);
				}
				if (z < maxZ && !info.BlocksLight[blocks[idx + width]] && light > (lightLevels[x, y, z + 1] & 0x0f)) {
					queue.Enqueue(idx + width, light);
				}
				if (y > 0 && !info.BlocksLight[blocks[idx - oneY]] && light > (lightLevels[x, y - 1, z] & 0x0f)) {
					queue.Enqueue(idx - oneY, light);
				}
				if (y < maxY && !info.BlocksLight[blocks[idx + oneY]] && light > (lightLevels[x, y + 1, z] & 0x0f)) {
					queue.Enqueue(idx + oneY, light);
				}
			}
		}
		
		class FastQueue {
			int[] idx_buffer;
			byte[] light_buffer;
			int head, tail;
			public int count;

			public FastQueue(int capacity) {
				idx_buffer = new int[capacity];
				light_buffer = new byte[capacity];
			}

			public void Enqueue(int idx, byte light) {
				if (count == idx_buffer.Length) {
					int newSize = (int)(idx_buffer.Length * 2);
					if (newSize < idx_buffer.Length + 4) {
						newSize = idx_buffer.Length + 4;
					}
					SetCapacity(newSize);
				}
				
				idx_buffer[tail] = idx;
				light_buffer[tail] = light;
				
				tail = (tail + 1) % idx_buffer.Length;
				count++;
			}

			public void Dequeue(out int idx, out byte light) {
				idx = idx_buffer[head];
				light = light_buffer[head];
				
				head = (head + 1) % idx_buffer.Length;
				count--;
			}

			void SetCapacity(int capacity) {
				int[] idx_new = new int[capacity];
				byte[] light_new = new byte[capacity];
				
				if (head < tail) {
					Array.Copy(idx_buffer, head, idx_new, 0, count);
					Array.Copy(light_buffer, head, light_new, 0, count);
				} else {
					Array.Copy(idx_buffer, head, idx_new, 0, idx_buffer.Length - head);
					Array.Copy(idx_buffer, 0, idx_new, idx_buffer.Length - head, tail);
					
					Array.Copy(light_buffer, head, light_new, 0, light_buffer.Length - head);
					Array.Copy(light_buffer, 0, light_new, light_buffer.Length - head, tail);
				}
				
				idx_buffer = idx_new; light_buffer = light_new;
				
				head = 0;
				tail = ((count == capacity) ? 0 : count);
			}
		}
	}
}
