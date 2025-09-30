#include "PluginAPI.h"

#include "Game.h"
#include "Block.h"
#include "ExtMath.h"
#include "Vectors.h"
#include "Chat.h"
#include "Stream.h"
#include "TexturePack.h"
#include "World.h"
#include "String.h"


/*########################################################################################################################*
*------------------------------------------------------Dynamic imports----------------------------------------------------*
*#########################################################################################################################*/
// This is just to work around problems with normal dynamic linking
// - Importing CC_VAR forces mingw to use runtime relocation, which bloats the dll on Windows
// See the bottom of the file for the actual ugly importing
static void LoadSymbolsFromGame(void);

static struct _Atlas2DData* Atlas2D_;

static struct _BlockLists* Blocks_;
static FP_Block_UNSAFE_GetName Block_UNSAFE_GetName_;
#define GetTex(block, face) Blocks_->Textures[(block) * FACE_COUNT + (face)]

static FP_Chat_Add Chat_Add_;
static FP_Commands_Register Commands_Register_;

static FP_Random_Next Random_Next_;
static FP_Random_Seed Random_Seed_;
static CC_INLINE int Random_Range_(RNGState* rnd, int min, int max) {
	return min + Random_Next_(rnd, max - min);
}

static FP_String_AppendConst String_AppendConst_;
static FP_String_CaselessEqualsConst String_CaselessEqualsConst_;
static FP_String_Equals  String_Equals_;
static FP_String_Format1 String_Format1_;
static FP_String_Format2 String_Format2_;
static FP_String_Format3 String_Format3_;

static FP_Stream_CreateFile Stream_CreateFile_;

static struct _WorldData* World_;


/*########################################################################################################################*
*--------------------------------------------------------Obj exporter-----------------------------------------------------*
*#########################################################################################################################*/
static cc_bool all, mirror;
static struct Stream stream;
static cc_bool include[BLOCK_COUNT]; // which blocks to actually dump vertices of
static int texI[BLOCK_COUNT]; // index mapping for textures 
// buffer data written
static cc_uint8  buffer[17000];
static int bufferLen;
#define Buffer_Str(str, len) str.buffer = (char*)&buffer[bufferLen]; str.length = 0; str.capacity = len;

static void Obj_Init(void) {
	const static cc_string invalid = String_FromConst("Invalid");

	// exports blocks that are not gas draw (air) and are not named "Invalid"
	for (int b = 0; b < BLOCK_COUNT; b++) 
	{
		cc_string name = Block_UNSAFE_GetName_(b);
		include[b] = Blocks_->Draw[b] != DRAW_GAS && !String_Equals_(&name, &invalid);
	}
	bufferLen = 0;
}

static void FlushData(int len) {
	bufferLen += len;
	if (bufferLen < 16384) return;
	cc_uint32 modified;

	stream.Write(&stream, buffer, bufferLen, &modified);
	bufferLen = 0;
}

static void WriteConst(const char* src) {
	cc_string tmp; Buffer_Str(tmp, 128);
	String_AppendConst_(&tmp, src);
	FlushData(tmp.length);
}

#define InitVars()\
	oneX = 1;          maxX = World_->MaxX; width  = World_->Width;\
	oneZ = width;      maxZ = World_->MaxZ; length = World_->Length;\
	oneY = World_->OneY; maxY = World_->MaxY; height = World_->Height;\
	blocks = World_->Blocks; blocks2 = World_->Blocks2;\
	mask = blocks == blocks2 ? 255 : 1023;

static void DumpNormals(void) {
	WriteConst("#normals\n");
	WriteConst("vn -1.0 0.0 0.0\n");
	WriteConst("vn 1.0 0.0 0.0\n");
	WriteConst("vn 0.0 0.0 -1.0\n");
	WriteConst("vn 0.0 0.0 1.0\n");
	WriteConst("vn 0.0 -1.0 0.0\n");
	WriteConst("vn 0.0 1.0 0.0\n");
	WriteConst("#sprite normals\n");
	WriteConst("vn -0.70710678 0 0.70710678\n");
	WriteConst("vn 0.70710678 0 -0.70710678\n");
	WriteConst("vn 0.70710678 0 0.70710678\n");
	WriteConst("vn -0.70710678 0 -0.70710678\n");
}


static void Unpack(int texLoc, int* x, int* y) {
	*x = (texLoc % ATLAS2D_TILES_PER_ROW);
	*y = (Atlas2D_->RowsCount - 1) - (texLoc / ATLAS2D_TILES_PER_ROW);
}

static void WriteTex(float u, float v) {
	cc_string tmp; Buffer_Str(tmp, 128);
	String_Format2_(&tmp, "vt %f8 %f8\n", &u, &v);
	FlushData(tmp.length);
}

static void WriteTexName(int b) {
	cc_string name = Block_UNSAFE_GetName_(b);
	cc_string tmp; Buffer_Str(tmp, 128);
	String_Format1_(&tmp, "#%s\n", &name);
	FlushData(tmp.length);
}

static void DumpTextures() {
	WriteConst("#textures\n");
	int i = 1;
	int x, y;
	float u = 1.0f / 16, v = 1.0f / Atlas2D_->RowsCount;

	for (int b = 0; b < BLOCK_COUNT; b++) {
		if (!include[b]) continue;
		WriteTexName(b);

		Vec3 min = Blocks_->MinBB[b], max = Blocks_->MaxBB[b];	
		if (Blocks_->Draw[b] == DRAW_SPRITE) {
			Vec3_Set(min, 0,0,0);
			Vec3_Set(max, 1,1,1);
		}
		texI[b] = i;

		Unpack(GetTex(b, FACE_XMIN), &x, &y);
		WriteTex((x + min.Z) * u, (y + min.Y) * v);
		WriteTex((x + min.Z) * u, (y + max.Y) * v);
		WriteTex((x + max.Z) * u, (y + max.Y) * v);
		WriteTex((x + max.Z) * u, (y + min.Y) * v);

		Unpack(GetTex(b, FACE_XMAX), &x, &y);
		WriteTex((x + max.Z) * u, (y + min.Y) * v);
		WriteTex((x + max.Z) * u, (y + max.Y) * v);
		WriteTex((x + min.Z) * u, (y + max.Y) * v);
		WriteTex((x + min.Z) * u, (y + min.Y) * v);

		Unpack(GetTex(b, FACE_ZMIN), &x, &y);
		WriteTex((x + max.X) * u, (y + min.Y) * v);
		WriteTex((x + max.X) * u, (y + max.Y) * v);
		WriteTex((x + min.X) * u, (y + max.Y) * v);
		WriteTex((x + min.X) * u, (y + min.Y) * v);

		Unpack(GetTex(b, FACE_ZMAX), &x, &y);
		WriteTex((x + min.X) * u, (y + min.Y) * v);
		WriteTex((x + min.X) * u, (y + max.Y) * v);
		WriteTex((x + max.X) * u, (y + max.Y) * v);
		WriteTex((x + max.X) * u, (y + min.Y) * v);

		Unpack(GetTex(b, FACE_YMIN), &x, &y);
		WriteTex((x + min.X) * u, (y + max.Z) * v);
		WriteTex((x + min.X) * u, (y + min.Z) * v);
		WriteTex((x + max.X) * u, (y + min.Z) * v);
		WriteTex((x + max.X) * u, (y + max.Z) * v);

		Unpack(GetTex(b, FACE_YMAX), &x, &y);
		WriteTex((x + min.X) * u, (y + max.Z) * v);
		WriteTex((x + min.X) * u, (y + min.Z) * v);
		WriteTex((x + max.X) * u, (y + min.Z) * v);
		WriteTex((x + max.X) * u, (y + max.Z) * v);
		i += 4 * 6;
	}
}

static cc_bool IsFaceHidden(int block, int other, int side) {
	return (Blocks_->Hidden[(block * BLOCK_COUNT) + other] & (1 << side)) != 0;
}

static void WriteVertex(float x, float y, float z) {
	cc_string tmp; Buffer_Str(tmp, 256);
	String_Format3_(&tmp, "v %f8 %f8 %f8\n", &x, &y, &z);
	FlushData(tmp.length);
}

static RNGState spriteRng;
static void DumpVertices() {
	WriteConst("#vertices\n");
	int i = -1, mask;
	Vec3 min, max;

	int oneX, maxX, width;
	int oneY, maxY, height;
	int oneZ, maxZ, length;
	cc_uint8 *blocks, *blocks2;
	InitVars();

	for (int y = 0; y < height; y++) {
		for (int z = 0; z < length; z++) {
			for (int x = 0; x < width; x++) {
				++i; int b = (blocks[i] | (blocks2[i] << 8)) & mask;
				if (!include[b]) continue;
				min.X = x; min.Y = y; min.Z = z;
				max.X = x; max.Y = y; max.Z = z;

				if (Blocks_->Draw[b] == DRAW_SPRITE) {
					min.X += 2.50f / 16; min.Z += 2.50f / 16;
					max.X += 13.5f / 16; max.Z += 13.5f / 16; max.Y += 1.0f;

					int offsetType = Blocks_->SpriteOffset[b];
					if (offsetType >= 6 && offsetType <= 7) {
						Random_Seed_(&spriteRng, (x + 1217 * z) & 0x7fffffff);
						float valX = Random_Range_(&spriteRng, -3, 3 + 1) / 16.0f;
						float valY = Random_Range_(&spriteRng,  0, 3 + 1) / 16.0f;
						float valZ = Random_Range_(&spriteRng, -3, 3 + 1) / 16.0f;

						const float stretch = 1.7f / 16.0f;
						min.X += valX - stretch; max.X += valX + stretch;
						min.Z += valZ - stretch; max.Z += valZ + stretch;
						if (offsetType == 7) { min.Y -= valY; max.Y -= valY; }
					}

					// Draw Z axis
					WriteVertex(min.X, min.Y, min.Z);
					WriteVertex(min.X, max.Y, min.Z);
					WriteVertex(max.X, max.Y, max.Z);
					WriteVertex(max.X, min.Y, max.Z);

					// Draw Z axis mirrored
					if (mirror) {
						WriteVertex(max.X, min.Y, max.Z);
						WriteVertex(max.X, max.Y, max.Z);
						WriteVertex(min.X, max.Y, min.Z);
						WriteVertex(min.X, min.Y, min.Z);
					}

					// Draw X axis
					WriteVertex(min.X, min.Y, max.Z);
					WriteVertex(min.X, max.Y, max.Z);
					WriteVertex(max.X, max.Y, min.Z);
					WriteVertex(max.X, min.Y, min.Z);

					// Draw X axis mirrored
					if (mirror) {
						WriteVertex(max.X, min.Y, min.Z);
						WriteVertex(max.X, max.Y, min.Z);
						WriteVertex(min.X, max.Y, max.Z);
						WriteVertex(min.X, min.Y, max.Z);
					}
					continue;
				}

				Vec3_AddBy(&min, &Blocks_->RenderMinBB[b]);
				Vec3_AddBy(&max, &Blocks_->RenderMaxBB[b]);

				// minx
				if (x == 0 || all || !IsFaceHidden(b, (blocks[i - oneX] | (blocks2[i - oneX] << 8)) & mask, FACE_XMIN)) {
					WriteVertex(min.X, min.Y, min.Z);
					WriteVertex(min.X, max.Y, min.Z);
					WriteVertex(min.X, max.Y, max.Z);
					WriteVertex(min.X, min.Y, max.Z);
				}

				// maxx
				if (x == maxX || all || !IsFaceHidden(b, (blocks[i + oneX] | (blocks2[i + oneX] << 8)) & mask, FACE_XMAX)) {
					WriteVertex(max.X, min.Y, min.Z);
					WriteVertex(max.X, max.Y, min.Z);
					WriteVertex(max.X, max.Y, max.Z);
					WriteVertex(max.X, min.Y, max.Z);
				}

				// minz
				if (z == 0 || all || !IsFaceHidden(b, (blocks[i - oneZ] | (blocks2[i - oneZ] << 8)) & mask, FACE_ZMIN)) {
					WriteVertex(min.X, min.Y, min.Z);
					WriteVertex(min.X, max.Y, min.Z);
					WriteVertex(max.X, max.Y, min.Z);
					WriteVertex(max.X, min.Y, min.Z);
				}

				// maxz
				if (z == maxZ || all || !IsFaceHidden(b, (blocks[i + oneZ] | (blocks2[i + oneZ] << 8)) & mask, FACE_ZMAX)) {
					WriteVertex(min.X, min.Y, max.Z);
					WriteVertex(min.X, max.Y, max.Z);
					WriteVertex(max.X, max.Y, max.Z);
					WriteVertex(max.X, min.Y, max.Z);
				}

				// miny
				if (y == 0 || all || !IsFaceHidden(b, (blocks[i - oneY] | (blocks2[i - oneY] << 8)) & mask, FACE_YMIN)) {
					WriteVertex(min.X, min.Y, min.Z);
					WriteVertex(min.X, min.Y, max.Z);
					WriteVertex(max.X, min.Y, max.Z);
					WriteVertex(max.X, min.Y, min.Z);
				}

				// maxy
				if (y == maxY || all || !IsFaceHidden(b, (blocks[i + oneY] | (blocks2[i + oneY] << 8)) & mask, FACE_YMAX)) {
					WriteVertex(min.X, max.Y, min.Z);
					WriteVertex(min.X, max.Y, max.Z);
					WriteVertex(max.X, max.Y, max.Z);
					WriteVertex(max.X, max.Y, min.Z);
				}
			}
		}
	}
}

static void WriteFace(int v1,int t1,int n1, int v2,int t2,int n2, int v3,int t3,int n3, int v4,int t4,int n4) {
	cc_string tmp; Buffer_Str(tmp, 256);
	String_Format3_(&tmp,"f %i/%i/%i ", &v1, &t1, &n1);
	String_Format3_(&tmp,  "%i/%i/%i ", &v2, &t2, &n2);
	String_Format3_(&tmp,  "%i/%i/%i ", &v3, &t3, &n3);
	String_Format3_(&tmp,  "%i/%i/%i\n",&v4, &t4, &n4);
	FlushData(tmp.length);
}

static void DumpFaces() {
	WriteConst("#faces\n");
	int i = -1, j = 1, mask;

	int oneX, maxX, width;
	int oneY, maxY, height;
	int oneZ, maxZ, length;
	cc_uint8 *blocks, *blocks2;
	InitVars();

	for (int y = 0; y < height; y++) {
		for (int z = 0; z < length; z++) {
			for (int x = 0; x < width; x++) {
				++i; int b = (blocks[i] | (blocks2[i] << 8)) & mask;
				if (!include[b]) continue;
				int k = texI[b], n = 1;

				if (Blocks_->Draw[b] == DRAW_SPRITE) {
					n += 6;
					WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4; n++;
					if (mirror) { WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4; n++; }

					WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4; n++;
					if (mirror) { WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4; n++; }
					continue;
				}

				// minx
				if (x == 0 || all || !IsFaceHidden(b, (blocks[i - oneX] | (blocks2[i - oneX] << 8)) & mask, FACE_XMIN)) {
					WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4;
				} k += 4; n++;

				// maxx
				if (x == maxX || all || !IsFaceHidden(b, (blocks[i + oneX] | (blocks2[i + oneX] << 8)) & mask, FACE_XMAX)) {
					WriteFace(j+0,k+0,n, j+1,k+1,n, j+2,k+2,n, j+3,k+3,n); j += 4;
				} k += 4; n++;

				// minz
				if (z == 0 || all || !IsFaceHidden(b, (blocks[i - oneZ] | (blocks2[i - oneZ] << 8)) & mask, FACE_ZMIN)) {
					WriteFace(j+0,k+0,n, j+1,k+1,n, j+2,k+2,n, j+3,k+3,n); j += 4;
				} k += 4; n++;

				// maxz
				if (z == maxZ || all || !IsFaceHidden(b, (blocks[i + oneZ] | (blocks2[i + oneZ] << 8)) & mask, FACE_ZMAX)) {
					WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4;
				} k += 4; n++;

				// miny
				if (y == 0 || all || !IsFaceHidden(b, (blocks[i - oneY] | (blocks2[i - oneY] << 8)) & mask, FACE_YMIN)) {
					WriteFace(j+3,k+3,n, j+2,k+2,n, j+1,k+1,n, j+0,k+0,n); j += 4;
				} k += 4; n++;

				// maxy
				if (y == maxY || all || !IsFaceHidden(b, (blocks[i + oneY] | (blocks2[i + oneY] << 8)) & mask, FACE_YMAX)) {
					WriteFace(j+0,k+0,n, j+1,k+1,n, j+2,k+2,n, j+3,k+3,n); j += 4;
				} k += 4; n++;
			}
		}
	}
}

static void ExportObj(void) {
	Obj_Init();
	DumpNormals();
	DumpTextures();
	DumpVertices();
	DumpFaces();

	if (!bufferLen) return;
	cc_uint32 modified;

	stream.Write(&stream, buffer, bufferLen, &modified);
	bufferLen = 0;
}

/*########################################################################################################################*
*---------------------------------------------------Plugin implementation-------------------------------------------------*
*#########################################################################################################################*/
#define SendChat(msg) const static cc_string str = String_FromConst(msg); Chat_Add_(&str);

static void ObjExporterCommand_Execute(const cc_string* args, int argsCount) {
	if (!argsCount) { SendChat("&cFilename required."); return; }

	char strBuffer[FILENAME_SIZE];
	cc_string str = String_FromArray(strBuffer);
	String_Format1_(&str, "maps/%s.obj", &args[0]);

	cc_result res = Stream_CreateFile_(&stream, &str);
	if (res) {
		str.length = 0;
		String_Format2_(&str, "error %h creating maps/%s.obj", &res, &args[0]);
		Chat_Add_(&str);
		return;
	}

	all = argsCount > 1 && String_CaselessEqualsConst_(&args[1], "ALL");
	if (all) { SendChat("&cExporting all faces - slow!"); }

	mirror = argsCount <= 2 || !String_CaselessEqualsConst_(&args[2], "NO");
	if (!mirror) { SendChat("&cNot mirroring sprites!"); }

	ExportObj();
	stream.Close(&stream);

	str.length = 0;
	String_Format1_(&str, "&eExported map to maps/%s.obj", &args[0]);
	Chat_Add_(&str);
}

static struct ChatCommand ObjExporterCommand = {
	"ObjExport", ObjExporterCommand_Execute, false,
	{
		"&a/client objexport [filename] ['all' for all faces] ['no' for not mirror]",
		"&eExports the current map to the OBJ file format, as [filename].obj.",
		"&e  (excluding faces of blocks named 'Invalid')",
		"&eThis can then be imported into 3D modelling software such as Blender.",
	}
};

static void ObjExporter_Init(void) {
	LoadSymbolsFromGame();
	Commands_Register_(&ObjExporterCommand);
}


/*########################################################################################################################*
*----------------------------------------------------Plugin boilerplate---------------------------------------------------*
*#########################################################################################################################*/
PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = {
	ObjExporter_Init /* Init */
};



/*########################################################################################################################*
*------------------------------------------------------Dynamic loading----------------------------------------------------*
*#########################################################################################################################*/
#define QUOTE(x) #x

#ifdef CC_BUILD_WIN
#define WIN32_LEAN_AND_MEAN
#define NOSERVICE
#define NOMCX
#define NOIME
#include <windows.h>
#define LoadSymbol(name) name ## _ = GetProcAddress(GetModuleHandleA(NULL), QUOTE(name))
#else
#include <dlfcn.h>
#define LoadSymbol(name) name ## _ = dlsym(0, QUOTE(name))
#endif

static void LoadSymbolsFromGame(void) {
	LoadSymbol(Atlas2D); 
	
	LoadSymbol(Blocks);
	LoadSymbol(Block_UNSAFE_GetName);
	
	LoadSymbol(Chat_Add);
	LoadSymbol(Commands_Register);
	
	LoadSymbol(Random_Seed);
	LoadSymbol(Random_Next);
	
	LoadSymbol(String_AppendConst);
	LoadSymbol(String_CaselessEqualsConst);
	LoadSymbol(String_Equals);
	LoadSymbol(String_Format1);
	LoadSymbol(String_Format2);
	LoadSymbol(String_Format3);
	
	LoadSymbol(Stream_CreateFile);
	
	LoadSymbol(World);
}
