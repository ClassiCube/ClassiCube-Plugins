// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using ClassicalSharp;
using ClassicalSharp.Events;
using ClassicalSharp.Map;

namespace VolumeLightingPlugin {
	
	/// <summary> Manages lighting through a simple heightmap, where each block is either in sun or shadow. </summary>
	public sealed partial class VolumeLighting : IWorldLighting {
		
		bool[] lightPasses = new bool[Block.Count];
		
		void CastInitial(int startX, int startZ, int endX, int endZ) {
			//initial loop for making fullbright spots
			int oneY = width * length, maxY = height - 1;
			byte[] blocks = game.World.blocks;
			
			FastQueue block_queue = new FastQueue(32 * 32 * 32);
			FastQueue sun_queue = new FastQueue(32 * 32 * 32);
			for (int i = 0; i < lightPasses.Length; i++) {
				// Light passes through a block if a) doesn't block light b) block isn't full block
				lightPasses[i] =
					!game.BlockInfo.BlocksLight[i] ||
					game.BlockInfo.MinBB[i] != OpenTK.Vector3.Zero ||
					game.BlockInfo.MaxBB[i] != OpenTK.Vector3.One;
			}
			
			for( int z = startZ; z < endZ; z++ ) {
				int horOffset = startX + (z * width);
				for( int x = startX; x < endX; x++ ) {
					int index = (maxY * oneY) + horOffset;
					horOffset++; // increase horizontal position
					if (lightPasses[blocks[index]]) {
						sun_queue.Enqueue(index);
						lightLevels[x, maxY, z] &= 0x0F;
						lightLevels[x, maxY, z] |= 0xF0;
					}
					
					for( int y = maxY; y >= 0; y-- ) {
						//if the current block is fullbright assign the fullest block brightness to the higher 4 bits
						if( info.FullBright[blocks[index]] ) {
							block_queue.Enqueue(index);
							lightLevels[x, y, z] &= 0xF0;
							lightLevels[x, y, z] |= 0x0F;
						}
						index -= oneY; // reduce y position
					}
				}
			}

			Console.BufferHeight = short.MaxValue - 10;
			Console.WriteLine(sun_queue.count + " - " + block_queue.count);
			CastLight(sun_queue, true, 0xF0,    16);
			CastLight(block_queue, false, 0x0F, 1);
		}

		void CastLight(FastQueue queue, bool sunlight, byte mask, byte step) {
			BlockInfo info = game.BlockInfo;
			byte[] blocks = game.World.blocks;
			int maxX = width - 1, maxY = height - 1, maxZ = length - 1;
			int oneY = width * length;
			byte invMask = (byte)~mask;
			
			while (queue.count > 0) {
				int idx; queue.Dequeue(out idx);

				int x = idx % width;
				int y = idx / oneY;
				int z = (idx / width) % length;
				byte light = lightLevels[x, y, z];
								
				if (light == step) break; // doesn't cast further
				light -= step;
				
				if (x > 0 && lightPasses[blocks[idx - 1]] && light > (lightLevels[x - 1, y, z] & mask)) {
					lightLevels[x - 1, y, z] &= invMask;
					lightLevels[x - 1, y, z] |= light;
					queue.Enqueue(idx - 1);
				}
				if (x < maxX && lightPasses[blocks[idx + 1]] && light > (lightLevels[x + 1, y, z] & mask)) {
					lightLevels[x + 1, y, z] &= invMask;
					lightLevels[x + 1, y, z] |= light;
					queue.Enqueue(idx + 1);
				}
				if (z > 0 && lightPasses[blocks[idx - width]] && light > (lightLevels[x, y, z - 1] & mask)) {
					lightLevels[x, y, z - 1] &= invMask;
					lightLevels[x, y, z - 1] |= light;
					queue.Enqueue(idx - width);
				}
				if (z < maxZ && lightPasses[blocks[idx + width]] && light > (lightLevels[x, y, z + 1] & mask)) {
					lightLevels[x, y, z + 1] &= invMask;
					lightLevels[x, y, z + 1] |= light;
					queue.Enqueue(idx + width);
				}
				
				if (y < maxY && lightPasses[blocks[idx + oneY]] && light > (lightLevels[x, y + 1, z] & mask)) {
					lightLevels[x, y + 1, z] &= invMask;
					lightLevels[x, y + 1, z] |= light;
				}
				
				// sunlight should propagate downwards without losing light
				if (sunlight && light == 0xE0) light += step;
				
				if (y > 0 && lightPasses[blocks[idx - oneY]] && light > (lightLevels[x, y - 1, z] & mask)) {
					lightLevels[x, y - 1, z] &= invMask;
					lightLevels[x, y - 1, z] |= light;
					queue.Enqueue(idx - oneY);
				}
			}
		}
		
		class FastQueue {
			int[] idx_buffer;
			int head, tail;
			public int count;

			public FastQueue(int capacity) {
				idx_buffer = new int[capacity];
			}

			public void Enqueue(int idx) {
				if (count == idx_buffer.Length) {
					int newSize = (int)(idx_buffer.Length * 2);
					if (newSize < idx_buffer.Length + 4) {
						newSize = idx_buffer.Length + 4;
					}
					SetCapacity(newSize);
				}
				
				idx_buffer[tail] = idx;
				tail = (tail + 1) % idx_buffer.Length;
				count++;
			}

			public void Dequeue(out int idx) {
				idx = idx_buffer[head];
				head = (head + 1) % idx_buffer.Length;
				count--;
			}

			void SetCapacity(int capacity) {
				int[] idx_new = new int[capacity];
				
				if (head < tail) {
					Array.Copy(idx_buffer, head, idx_new, 0, count);
				} else {
					Array.Copy(idx_buffer, head, idx_new, 0, idx_buffer.Length - head);
					Array.Copy(idx_buffer, 0, idx_new, idx_buffer.Length - head, tail);
				}
				
				idx_buffer = idx_new;
				head = 0;
				tail = ((count == capacity) ? 0 : count);
			}
		}
	}
}
