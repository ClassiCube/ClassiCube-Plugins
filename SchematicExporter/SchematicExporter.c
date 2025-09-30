#include "PluginAPI.h"

#include "World.h"
#include "Deflate.h"
#include "Stream.h"
#include "Chat.h"
#include "Game.h"
#include "Funcs.h"
#include "String.h"


/*########################################################################################################################*
*------------------------------------------------------Dynamic imports----------------------------------------------------*
*#########################################################################################################################*/
// This is just to work around problems with normal dynamic linking
// - Importing CC_VAR forces mingw to use runtime relocation, which bloats the dll (twice the size) on Windows
// See the bottom of the file for the actual ugly importing
static void LoadSymbolsFromGame(void);

static FP_Chat_Add Chat_Add_;
static FP_Commands_Register Commands_Register_;

static FP_GZip_MakeStream GZip_MakeStream_;

static FP_String_Format1 String_Format1_;
static FP_String_Format2 String_Format2_;
static FP_String_Format3 String_Format3_;

static FP_Stream_CreateFile Stream_CreateFile_;
static FP_Stream_Write Stream_Write_;

static struct _WorldData* World_;


/*########################################################################################################################*
*--------------------------------------------------------Common utils-----------------------------------------------------*
*#########################################################################################################################*/
static void SendChat(const char* format, const void* arg1, const void* arg2, const void* arg3) {
	cc_string msg; char msgBuffer[256];
	String_InitArray(msg, msgBuffer);

	String_Format3_(&msg, format, arg1, arg2, arg3);
	Chat_Add_(&msg);
}

static void WarnChat(cc_result res, const char* place, const cc_string* path) {
	SendChat("Error %h when %c '%s'", &res, place, path);
}

static void SetU16_BE(cc_uint8* data, cc_uint16 value) {
	data[0] = (cc_uint8)(value >> 8 ); data[1] = (cc_uint8)(value);
}

static void SetU32_BE(cc_uint8* data, cc_uint32 value) {
	data[0] = (cc_uint8)(value >> 24); data[1] = (cc_uint8)(value >> 16);
	data[2] = (cc_uint8)(value >> 8 ); data[3] = (cc_uint8)(value);
}


/*########################################################################################################################*
*--------------------------------------------------------Lookup table-----------------------------------------------------*
*#########################################################################################################################*/
static const cc_uint8 beta_data[256] = {
/*  +0   +1   +2   +3   +4   +5   +6   +7   +8   +9   +10  +11  +12  +13  +14  +15 */
	  0,   1,   2,   3,   4,   5,   6,   7,   8,   9,  10,  11,  12,  13,  14,  15, /* 0   */
	 16,  17,  18,  19,  20,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35, /* 16  */
	 35,  35,  35,  35,  35,  37,  38,  39,  40,  41,  42,  43,  44,  45,  46,  47, /* 32  */
	 48,  49,  44,  75,  24,  78,  51,  35,  35,  35,  35,  35,  79, 169, 213, 155, /* 48  */
	 84,  98,  66,  67,  68,  69,  70,  71,  72,  73,  74,  75,  76,  77,  78,  79, /* 64  */
	 80,  81,  82,  83,  84,  85,  86,  87,  88,  89,  90,  91,  92,  93,  94,  95, /* 80  */
	 96,  97,  98,  99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, /* 96  */
	112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, /* 112 */
	128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, /* 128 */
	144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, /* 144 */
	160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, /* 160 */
	176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, /* 176 */
	192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, /* 192 */
	208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, /* 208 */
	224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, /* 224 */
	240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, /* 240 */
};

static const cc_uint8 beta_meta[256] = {
/*  +0   +1   +2   +3   +4   +5   +6   +7   +8   +9   +10  +11  +12  +13  +14  +15 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 0   */
	  0,   0,   0,   0,   0,  14,   1,   4,   5,  13,   3,   3,   3,  11,  10,  10, /* 16  */
	  2,   6,   7,   8,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 32  */
	  0,   0,   3,   5,   0,   0,   0,   6,  13,  12,  11,   9,   0,   0,   0,   2, /* 48  */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 64  */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 80  */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 96  */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 112 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 128 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 144 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 160 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 176 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 192 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 208 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 224 */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 240 */
};


/*########################################################################################################################*
*-------------------------------------------------------Format writing----------------------------------------------------*
*#########################################################################################################################*/
enum NbtTagType { 
	NBT_END, NBT_I8,  NBT_I16, NBT_I32,  NBT_I64,  NBT_F32, 
	NBT_R64, NBT_I8S, NBT_STR, NBT_LIST, NBT_DICT, NBT_I32S
};

static cc_uint8 sc_begin[76] = {
NBT_DICT, 0,9, 'S','c','h','e','m','a','t','i','c',
	NBT_STR,  0,9,  'M','a','t','e','r','i','a','l','s', 0,5, 'A','l','p','h','a',
	NBT_I16,  0,5,  'W','i','d','t','h',                 0,0,
	NBT_I16,  0,6,  'H','e','i','g','h','t',             0,0,
	NBT_I16,  0,6,  'L','e','n','g','t','h',             0,0,
	NBT_I8S,  0,6,  'B','l','o','c','k','s',             0,0,0,0,
};
static cc_uint8 sc_data[11] = {
	NBT_I8S,  0,4,  'D','a','t','a',                     0,0,0,0,
};
static cc_uint8 sc_end[37] = {
	NBT_LIST, 0,8,  'E','n','t','i','t','i','e','s',                 NBT_DICT, 0,0,0,0,
	NBT_LIST, 0,12, 'T','i','l','e','E','n','t','i','t','i','e','s', NBT_DICT, 0,0,0,0,
NBT_END,
};

static cc_result SaveSchematic(struct Stream* stream) {
	cc_uint8 chunk[8192];
	cc_result res;
	int i, j, count, volume = World_->Volume;
	cc_uint8* blocks = World_->Blocks;

	SetU16_BE(&sc_begin[39], World_->Width);
	SetU16_BE(&sc_begin[50], World_->Height);
	SetU16_BE(&sc_begin[61], World_->Length);
	SetU32_BE(&sc_begin[72], volume);
	if ((res = Stream_Write_(stream, sc_begin, sizeof(sc_begin)))) return res;
	
	for (i = 0; i < volume; i += sizeof(chunk)) 
	{
		count = volume - i; count = min(count, sizeof(chunk));

		for (j = 0; j < count; j++) { chunk[j] = beta_data[blocks[i + j]]; }
		if ((res = Stream_Write_(stream, chunk, count))) return res;
	}

	SetU32_BE(&sc_data[7], volume);
	if ((res = Stream_Write_(stream, sc_data, sizeof(sc_data)))) return res;

	for (i = 0; i < volume; i += sizeof(chunk)) 
	{
		count = volume - i; count = min(count, sizeof(chunk));

		for (j = 0; j < count; j++) { chunk[j] = beta_meta[blocks[i + j]]; }
		if ((res = Stream_Write_(stream, chunk, count))) return res;
	}

	return Stream_Write_(stream, sc_end, sizeof(sc_end));
}

static void SaveMap(const cc_string* path) {
	struct Stream stream, compStream;
	struct GZipState state;
	cc_result res;

	res = Stream_CreateFile_(&stream, path);
	if (res) { WarnChat(res, "creating", path); return; }
	GZip_MakeStream_(&compStream, &state, &stream);

	res = SaveSchematic(&compStream);

	if (res) {
		stream.Close(&stream);
		WarnChat(res, "encoding", path); return;
	}
		
	if ((res = compStream.Close(&compStream))) {
		stream.Close(&stream);
		WarnChat(res, "closing", path); return;
	}

	res = stream.Close(&stream);
	if (res) { WarnChat(res, "closing", path); return; }

	SendChat("&eExported map to: %s", path, NULL, NULL);
}



/*########################################################################################################################*
*---------------------------------------------------Plugin implementation-------------------------------------------------*
*#########################################################################################################################*/
static void SchematicExportCmd_Execute(const cc_string* args, int argsCount) {
	if (!argsCount) { SendChat("&cFilename required.", NULL, NULL, NULL); return; }
	char pathBuffer[FILENAME_SIZE];
	cc_string path = String_FromArray(pathBuffer);

	String_Format1_(&path, "maps/%s.schematic", args);
	SaveMap(&path);
}

static struct ChatCommand SchematicExportCmd = {
	"SchematicExport", SchematicExportCmd_Execute, false,
	{
		"&a/client schematicexport [filename]",
		"&eExports current map to the schematic file format, as [filename].schematic.",
		"&eThis can then be imported into software such as MCEdit.",
	}
};

static void SchematicExporter_Init(void) {
	LoadSymbolsFromGame();
	Commands_Register_(&SchematicExportCmd);
}


/*########################################################################################################################*
*----------------------------------------------------Plugin boilerplate---------------------------------------------------*
*#########################################################################################################################*/
PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = {
	SchematicExporter_Init /* Init */
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
#define _GNU_SOURCE
#include <dlfcn.h>
#define LoadSymbol(name) name ## _ = dlsym(RTLD_DEFAULT, QUOTE(name))
#endif

static void LoadSymbolsFromGame(void) {
	LoadSymbol(Chat_Add);
	LoadSymbol(Commands_Register);
	
	LoadSymbol(String_Format1);
	LoadSymbol(String_Format2);
	LoadSymbol(String_Format3);
	
	LoadSymbol(Stream_CreateFile);
	LoadSymbol(Stream_Write);
	
	LoadSymbol(GZip_MakeStream);
	
	LoadSymbol(World);
}
