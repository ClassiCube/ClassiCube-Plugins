using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;

namespace ClassicalSharp.GraphicsAPI {
	
	public unsafe sealed class SoftwareApi : IGraphicsApi {

		bool faceCulling;
		int width, height;

		bool alphaBlending;

		bool alphaTest;
		CompareFunc alphaTestFunc;
		byte alphaRef;

		int[] colBuffer;
		int clearCol = 0;
		bool colWrite = true;
		bool texturing;

		float[] depthBuffer;
		CompareFunc depthTestFunc;
		float clearDepth = 1;
		bool depthTest = true;
		bool depthWrite = true;
		
		public SoftwareApi( Game game ) {
			OnWindowResize( game );
			base.InitDynamicBuffers();
		}
		
		public override bool AlphaBlending {
			set { alphaBlending = value; }
		}
		
		public override void AlphaBlendFunc( BlendFunc srcFunc, BlendFunc destFunc ) {
		}
		
		public override bool AlphaTest {
			set { alphaTest = value; }
		}
		
		public override void AlphaTestFunc( CompareFunc func, float value ) {
			alphaTestFunc = func;
			alphaRef = (byte)( value * 255 );
		}
		
		public override void ClearColour(FastColour col) {
			clearCol = col.A << 24 | col.R << 16 | col.G << 8 | col.B;
		}
		
		public override bool ColourWrite {
			set { colWrite = value; }
		}
		
		public override bool DepthTest {
			set { depthTest = value; }
		}
		
		public override void DepthTestFunc( CompareFunc func ) {
			depthTestFunc = func;
		}
		
		public override bool DepthWrite {
			set { depthWrite = value; }
		}
		
		public override bool FaceCulling {
			set { faceCulling = value; }
		}
		
		public override int MaxTextureDimensions {
			get { return 65536; } // Okay we may be slightly exaggerating here.
		}
		
		public override void OnWindowResize( Game game ) {
			width = game.Width;
			height = game.Height;
			colBuffer = new int[width * height];
			depthBuffer = new float[width * height];
			Clear();
		}
		
		public unsafe override void TakeScreenshot(string output, int width, int height) {
			fixed( int* colPtr = colBuffer ) {
				using( Bitmap bmp = new Bitmap(width, height,
				                               width * 4, PixelFormat.Format32bppRgb, (IntPtr)colPtr ) ) {
					bmp.Save( output, ImageFormat.Png );
				}
			}
		}
		
		public override bool Texturing {
			set { texturing = value; }
		}
		
		public override void Clear() {
			for( int i = 0; i < colBuffer.Length; i++ ) {
				colBuffer[i] = clearCol;
			}
			for( int i = 0; i < depthBuffer.Length; i++ ) {
				depthBuffer[i] = clearDepth;
			}
		}
		
		public override void BeginFrame( Game game ) {

		}
		
		public unsafe override void EndFrame( Game game ) {
			// TODO: PRESENT
			OpenTK.Platform.IWindowInfo info = game.window.WindowInfo;
			using( Graphics g = Graphics.FromHwnd( info.WinHandle ) ) {
				fixed( int* colPtr = colBuffer ) {
					using( Bitmap bmp = new Bitmap( width, height, width * 4, PixelFormat.Format32bppRgb, (IntPtr)colPtr ) ) {
						g.DrawImage( bmp, 0, 0, width, height );
					}
				}
			}
		}
		
		#region Fog
		
		public override bool AlphaArgBlend { set { } }
		
		bool fog;
		public override bool Fog {
			get { return fog; } 
			set { fog = value; }
		}
		
		internal override void MakeApiInfo() { }
		
		public override void SetVSync(Game game, bool value) { }
		
		public override void SetFogColour( FastColour col ) {
		}

		public override void SetFogDensity( float value ) {
		}
		
		public override void SetFogStart( float value ) {
		}
		
		public override void SetFogEnd( float value ) {
		}
		
		public override void SetFogMode( Fog mode ) {
		}
		
		#endregion
		
		#region Matrices
		MatrixStack projStack = new MatrixStack( 4 );
		MatrixStack texStack = new MatrixStack( 4 );
		MatrixStack viewStack = new MatrixStack( 8 );
		MatrixStack curStack;
		Matrix4 mvp;
		
		public override void LoadIdentityMatrix() {
			Matrix4 m = Matrix4.Identity;
			LoadMatrix( ref m );
		}
		
		public override void LoadMatrix( ref Matrix4 matrix ) {
			curStack.SetTop( ref matrix );
			Matrix4.Mult( out mvp, ref projStack.Top, ref viewStack.Top);
		}
		
		public override void MultiplyMatrix( ref Matrix4 matrix ) {
			curStack.MultiplyTop( ref matrix );
			Matrix4.Mult( out mvp, ref projStack.Top, ref viewStack.Top);
		}
		
		public override void PopMatrix() {
			curStack.Pop();
		}
		
		public override void PushMatrix() {
			curStack.Push();
		}
		
		public override void SetMatrixMode( MatrixType mode ) {
			if( mode == MatrixType.Modelview )
				curStack = viewStack;
			else if( mode == MatrixType.Projection )
				curStack = projStack;
			else
				curStack = texStack;
		}
		
		class MatrixStack {
			Matrix4[] stack;
			int stackIndex;
			public Matrix4 Top;

			public MatrixStack( int capacity ) {
				stack = new Matrix4[capacity];
				stack[0] = Matrix4.Identity;
				Top = Matrix4.Identity;
			}

			public void Push() {
				stack[stackIndex + 1] = stack[stackIndex];
				stackIndex++;
			}

			public void SetTop( ref Matrix4 matrix ) {
				stack[stackIndex] = matrix;
				Top = stack[stackIndex];
			}

			public void MultiplyTop( ref Matrix4 matrix ) {
				Matrix4.Mult(out stack[stackIndex], ref matrix, ref stack[stackIndex]); // top = matrix * top
				Top = stack[stackIndex];
			}

			public void Pop() {
				stackIndex--;
				Top = stack[stackIndex];
			}
		}
		#endregion
		
		#region Textures
		class TexObject {
			public int[] Pixels;
			public int Width;
			public int Height;
		}
		const int texBufferSize = 512;
		TexObject[] textures = new TexObject[texBufferSize];
		TexObject curTexture;
		int[] curTexPixels;
		int curTexWidth, curTexHeight;
		
		public override void BindTexture( int texId ) {
			curTexture = textures[texId];
			curTexPixels = curTexture.Pixels;
			curTexWidth = curTexture.Width;
			curTexHeight = curTexture.Height;
		}
		
		public override void DeleteTexture( ref int texId ) {
			if( texId <= 0 || texId >= textures.Length ) return;
			textures[texId] = null;
			texId = -1;
		}
		
		protected unsafe override int CreateTexture(int width, int height, IntPtr scan0, bool managedPool, bool mipmaps) {
			int[] pixels = new int[width * height];
			fixed( int* texPtr = pixels ) {
				memcpy( scan0, (IntPtr)texPtr, width * height * 4 );
			}
			TexObject obj = new TexObject();
			obj.Pixels = pixels;
			obj.Width = width;
			obj.Height = height;
			return GetOrExpand( ref textures, obj, texBufferSize );
		}
		
		public override void UpdateTexturePart(int texId, int texX, int texY, FastBitmap part, bool mipmaps) {
			TexObject tex = textures[texId];
			fixed( int* texPtr = tex.Pixels ) {
				int* dst = texPtr + (texY * tex.Width) + texX;
				for (int y = 0; y < part.Height; y++) {
					memcpy((IntPtr)part.GetRowPtr(y), (IntPtr)dst, part.Width * 4);
					dst += part.Width;
				}
			}	
		}
		
		public override void EnableMipmaps() { }
		
		public override void DisableMipmaps() { }
		
		#endregion
		
		#region Index / Vertex buffers
		const int vBufferSize = 2048;
		byte[][] vBuffers = new byte[vBufferSize][];
		byte[] curVBuffer;
		const int iBufferSize = 2;
		ushort[][] iBuffers = new ushort[iBufferSize][];
		ushort[] curIBuffer;
		VertexFormat drawFormat;
		int drawStride;
		
		public override void SetBatchFormat( VertexFormat format ) {
			drawFormat = format;
			drawStride = strideSizes[(int)format];
		}
		
		public override int CreateDynamicVb( VertexFormat format, int maxVertices ) {
			byte[] buffer = new byte[maxVertices * strideSizes[(int)format]];
			return GetOrExpand( ref vBuffers, buffer, vBufferSize );
		}
		
		public override void DeleteIb( ref int id ) {
			if( id <= 0 || id >= iBuffers.Length ) return;
			iBuffers[id] = null;
		}
		
		public override void DeleteVb( ref int id ) {
			if( id <= 0 || id >= vBuffers.Length ) return;
			vBuffers[id] = null;
		}
		
		public override void BindVb(int vb) {
			curVBuffer = vBuffers[vb];
		}
		
		public override void BindIb(int ib) {
			curIBuffer = iBuffers[ib];
		}
		
		public override void SetDynamicVbData(int vb, IntPtr vertices, int vCount) {
			curVBuffer = vBuffers[vb];
			fixed( byte* dst = curVBuffer ) {
				memcpy( vertices, (IntPtr)dst, vCount * drawStride );
			}
		}
		
		public override void DrawVb_Lines(int verticesCount) {
			throw new NotImplementedException();
		}
		
		public override void DrawVb_IndexedTris(int indicesCount) {
			DrawIndexedVbInternal( 0, indicesCount );
		}
		
		public override void DrawVb_IndexedTris(int indicesCount, int startIndex) {
			DrawIndexedVbInternal( startIndex, indicesCount);
		}
		
		internal override void DrawIndexedVb_TrisT2fC4b(int indicesCount, int startIndex) {
			DrawIndexedVbInternal( startIndex, indicesCount);
		}
		
		public unsafe override int CreateIb( IntPtr indices, int indicesCount ) {
			ushort[] buffer = new ushort[indicesCount];
			fixed( ushort* dst = buffer ) {
				memcpy( indices, (IntPtr)dst, indicesCount * 2 );
			}
			return GetOrExpand( ref iBuffers, buffer, iBufferSize );
		}
		
		public unsafe override int CreateVb(IntPtr vertices, VertexFormat format, int count) {
			byte[] buffer = new byte[count * strideSizes[(int)format]];
			fixed( byte* dst = buffer ) {
				memcpy( vertices, (IntPtr)dst, buffer.Length );
			}
			return GetOrExpand( ref vBuffers, buffer, vBufferSize );
		}
		
		#endregion
		
		#region Vertex transformation
		
		public bool DEBUG_FRAGS;
		
		unsafe void DrawIndexedVbInternal( int startIndex, int indicesCount ) {
			fixed( byte* ptr = curVBuffer ) {
				Vector3 frag1, frag2, frag3;
				Vector2 uv1 = new Vector2(0, 0), uv2 = new Vector2(0, 0), uv3 = new Vector2(0, 0);
				int col1 = 0, col2 = 0, col3 = 0;
				int j = (startIndex / 6) * 4;
				
				for( int i = 0; i < indicesCount / 6; i++ ) {
					TransformVertex( ptr, j + 0, out frag1, ref uv1, ref col1 );
					TransformVertex( ptr, j + 1, out frag2, ref uv2, ref col2 );
					TransformVertex( ptr, j + 2, out frag3, ref uv3, ref col3 );
					if( !TriangleCulled( ref frag1, ref frag2, ref frag3 ) )
						drawTriangle( ref frag1, ref frag2, ref frag3, ref uv1, ref uv2, ref uv3, col1 );
					
					TransformVertex( ptr, j + 2, out frag1, ref uv1, ref col1 );
					TransformVertex( ptr, j + 3, out frag2, ref uv2, ref col2 );
					TransformVertex( ptr, j + 0, out frag3, ref uv3, ref col3 );
					if( !TriangleCulled( ref frag1, ref frag2, ref frag3 ) )
						drawTriangle( ref frag1, ref frag2, ref frag3, ref uv1, ref uv2, ref uv3, col1 );
					
					j += 4;
				}
			}
		}
		
		bool TriangleCulled( ref Vector3 frag1, ref Vector3 frag2, ref Vector3 frag3 ) {
			if( !faceCulling ) return false;
			
			Vector3 side1 = frag1 - frag2;
			Vector3 side2 = frag3 - frag2;
			Vector3 normal = Vector3.Cross( side1, side2 );
			return Vector3.Dot( normal, frag1 ) <= 0;
		}
		
		unsafe void TransformVertex( byte* ptr, int index, out Vector3 frag, ref Vector2 uv, ref int col ) {
			ptr += index * drawStride;
			float* posPtr = (float*)ptr;
			Vector4 coord = new Vector4( *posPtr++, *posPtr++, *posPtr++, 1 );
			Transform( ref coord, ref mvp, out coord );
			frag.X = width * 0.5f * ( 1 + coord.X / coord.W );
			frag.Y = height * 0.5f * ( 1 - coord.Y / coord.W );
			frag.Z = coord.Z / coord.W;
			
			switch( drawFormat ) {
				case VertexFormat.P3fC4b:
					col = *(int*)posPtr; posPtr++;
					break;
					
				case VertexFormat.P3fT2fC4b:
					col = *(int*)posPtr; posPtr++;
					uv.X = *posPtr++;
					uv.Y = *posPtr++;
					break;
			}
		}
		
		
		#endregion
		
		#region Rasterization

		void drawTriangle( ref Vector3 frag1, ref Vector3 frag2, ref Vector3 frag3,
		                  ref Vector2 uv1, ref Vector2 uv2, ref Vector2 uv3, int col ) {
			int x1 = (int)frag1.X, y1 = (int)frag1.Y;
			int x2 = (int)frag2.X, y2 = (int)frag2.Y;
			int x3 = (int)frag3.X, y3 = (int)frag3.Y;
			int minX = Math.Min( x1, Math.Min( x2, x3 ) );
			int minY = Math.Min( y1, Math.Min( y2, y3 ) );
			int maxX = Math.Max( x1, Math.Max( x2, x3 ) );
			int maxY = Math.Max( y1, Math.Max( y2, y3 ) );
			
			// Triangle is completely outside the visible frustum. Reject it.
			if( minX < 0 && maxX < 0 || minY < 0 && maxY < 0 ||
			   minX >= width && maxX >= width || minY >= height && maxY >= height )
				return;
			
			//minX = Math.Max( minX, 0 ); maxX = Math.Min( width - 1, maxX );
			//minY = Math.Max( minY, 0 ); maxY = Math.Min( height - 1, maxY );
			
			float factor = 1f / ( ( y2 - y3 ) * (x1 - x3) + ( x3 - x2 ) * (y1 - y3 ) );
			for( int y = minY; y <= maxY; y++ ) {
				for( int x = minX; x <= maxX; x++ ) {
					if( x < 0 || y < 0 || x >= width || y >= height ) return;
					
					float ic0 = ( (y2-y3)*(x-x3)+(x3-x2)*(y-y3) ) * factor;
					if ( ic0 < 0 || ic0 > 1 ) continue;
					float ic1 = ( (y3-y1)*(x-x3)+(x1-x3)*(y-y3) ) * factor;
					if ( ic1 < 0 || ic1 > 1 ) continue;
					float ic2 = 1f - ic0 - ic1;
					if ( ic2 < 0 || ic2 > 1 ) continue;
					
					int index = y * width + x;
					float z = 1 / ( ic0 * 1 / frag1.Z + ic1 * 1 / frag2.Z + ic2 * 1 / frag3.Z );
					
					if( z <= depthBuffer[index] ) {
						int fragCol = col;
						if( texturing ) {
							float u = ( ic0 * uv1.X / frag1.Z + ic1 * uv2.X / frag2.Z + ic2 * uv3.X / frag3.Z ) * z;
							float v = ( ic0 * uv1.Y / frag1.Z + ic1 * uv2.Y / frag2.Z + ic2 * uv3.Y / frag3.Z ) * z;
							int texX = (int)( Math.Abs( u - Math.Floor( u ) ) * curTexWidth );
							int texY = (int)( Math.Abs( v - Math.Floor( v ) ) * curTexHeight );
							int texIndex = texY * curTexWidth + texX;
							bool reject;
							fragCol = MultiplyColours( (uint)fragCol, (uint)curTexPixels[texIndex], out reject );
							if( reject ) continue;
						}
						if( depthWrite ) {
							depthBuffer[index] = z;
						}
						colBuffer[index] = fragCol;
					}
				}
			}
		}
		
		int MultiplyColours( uint col1, uint col2, out bool reject ) {
			uint a1 = ( col1 & 0xFF000000 ) >> 24, a2 = ( col2 & 0xFF000000 ) >> 24;
			uint a = ( a1 * a2 ) / 255;
			reject = alphaTest && a < alphaRef;
			
			uint r1 = ( col1 & 0xFF0000 ) >> 16, r2 = ( col2 & 0xFF0000 ) >> 16;
			uint r = ( r1 * r2 ) / 255;
			uint g1 = ( col1 & 0xFF00 ) >> 8, g2 = ( col2 & 0xFF00 ) >> 8;
			uint g = ( g1 * g2 ) / 255;
			uint b1 = col1 & 0xFF, b2 = col2 & 0xFF;
			uint b = ( b1 * b2 ) / 255;
			return (int)( a << 24 | r << 16 | g << 8 | b );
		}

		#endregion
		
		static int GetOrExpand<T>( ref T[] array, T value, int expSize ) {
			// Find first free slot
			for( int i = 1; i < array.Length; i++ ) {
				if( array[i] == null ) {
					array[i] = value;
					return i;
				}
			}
			// Otherwise resize and add more elements
			int oldLength = array.Length;
			Array.Resize( ref array, array.Length + expSize );
			array[oldLength] = value;
			return oldLength;
		}
		
		unsafe void memcpy( IntPtr sourcePtr, IntPtr destPtr, int bytes ) {
			byte* src = (byte*)sourcePtr;
			byte* dst = (byte*)destPtr;
			int* srcInt = (int*)src;
			int* dstInt = (int*)dst;

			while( bytes >= 4 ) {
				*dstInt++ = *srcInt++;
				dst += 4;
				src += 4;
				bytes -= 4;
			}
			// Handle non-aligned last few bytes.
			for( int i = 0; i < bytes; i++ ) {
				*dst++ = *src++;
			}
		}    
		
		static void Transform(ref Vector4 vec, ref Matrix4 mat, out Vector4 result) {
			result = new Vector4(
				mat.Row0.X * vec.X + mat.Row0.Y * vec.Y + mat.Row0.Z * vec.Z + mat.Row0.W * vec.W,
				mat.Row1.X * vec.X + mat.Row1.Y * vec.Y + mat.Row1.Z * vec.Z + mat.Row1.W * vec.W,
				mat.Row2.X * vec.X + mat.Row2.Y * vec.Y + mat.Row2.Z * vec.Z + mat.Row2.W * vec.W,
				mat.Row3.X * vec.X + mat.Row3.Y * vec.Y + mat.Row3.Z * vec.Z + mat.Row3.W * vec.W);
		}
	}
}
