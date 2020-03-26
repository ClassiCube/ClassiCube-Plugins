#include "World.h"
#include "Deflate.h"
#include "Stream.h"
#include "Chat.h"
#include "GameStructs.h"
#include "Funcs.h"

enum NbtTagType { 
	NBT_END, NBT_I8,  NBT_I16, NBT_I32,  NBT_I64,  NBT_F32, 
	NBT_R64, NBT_I8S, NBT_STR, NBT_LIST, NBT_DICT, NBT_I32S
};

static const cc_uint8 sc_begin[76] = {
NBT_DICT, 0,9, 'S','c','h','e','m','a','t','i','c',
	NBT_STR,  0,9,  'M','a','t','e','r','i','a','l','s', 0,5, 'A','l','p','h','a',
	NBT_I16,  0,5,  'W','i','d','t','h',                 0,0,
	NBT_I16,  0,6,  'H','e','i','g','h','t',             0,0,
	NBT_I16,  0,6,  'L','e','n','g','t','h',             0,0,
	NBT_I8S,  0,6,  'B','l','o','c','k','s',             0,0,0,0,
};
static const cc_uint8 sc_data[11] = {
	NBT_I8S,  0,4,  'D','a','t','a',                     0,0,0,0,
};
static const cc_uint8 sc_end[37] = {
	NBT_LIST, 0,8,  'E','n','t','i','t','i','e','s',                 NBT_DICT, 0,0,0,0,
	NBT_LIST, 0,12, 'T','i','l','e','E','n','t','i','t','i','e','s', NBT_DICT, 0,0,0,0,
NBT_END,
};

static const cc_uint8 beta_data[256] = {
/*  -0-  -1-  -2-  -3-  -4-  -5-  -6-  -7-  -8-  -9-  -A-  -B-  -C-  -D-  -E-  -F- */
	  0,   1,   2,   3,   4,   5,   6,   7,   8,   9,  10,  11,  12,  13,  14,  15, /* 0X */
	 16,  17,  18,  19,  20,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35,  35, /* 1X */
	 35,  35,  35,  35,  35,  37,  38,  39,  40,  41,  42,  43,  44,  45,  46,  47, /* 2X */
	 48,  49,  44,  75,  24,  78,  51,  35,  35,  35,  35,  35,  79, 169, 213, 155, /* 3X */
	 84,  98,  66,  67,  68,  69,  70,  71,  72,  73,  74,  75,  76,  77,  78,  79, /* 4X */
	 80,  81,  82,  83,  84,  85,  86,  87,  88,  89,  90,  91,  92,  93,  94,  95, /* 5X */
	 96,  97,  98,  99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, /* 6X */
	112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, /* 7X */
	128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, /* 8X */
	144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, /* 9X */
	160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, /* AX */
	176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, /* BX */
	192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, /* CX */
	208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, /* DX */
	224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, /* EX */
	240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, /* FX */
};

static const cc_uint8 beta_meta[256] = {
/*  -0-  -1-  -2-  -3-  -4-  -5-  -6-  -7-  -8-  -9-  -A-  -B-  -C-  -D-  -E-  -F- */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 0X */
	  0,   0,   0,   0,   0,  14,   1,   4,   5,  13,   3,   3,   3,  11,  10,  10, /* 1X */
	  2,   6,   7,   8,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 2X */
	  0,   0,   3,   5,   0,   0,   0,   6,  13,  12,  11,   9,   0,   0,   0,   2, /* 3X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 4X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 5X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 6X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 7X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 8X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* 9X */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* AX */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* BX */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* CX */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* DX */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* EX */
	  0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0, /* FX */
};

static cc_result SaveSchematic(struct Stream* stream) {
	cc_uint8 tmp[256], chunk[8192] = { 0 };
	cc_result res;
	int i, j, count;

	Mem_Copy(tmp, sc_begin, sizeof(sc_begin));
	{
		Stream_SetU16_BE(&tmp[39], World.Width);
		Stream_SetU16_BE(&tmp[50], World.Height);
		Stream_SetU16_BE(&tmp[61], World.Length);
		Stream_SetU32_BE(&tmp[72], World.Volume);
	}
	if ((res = Stream_Write(stream, tmp, sizeof(sc_begin)))) return res;
	
	for (i = 0; i < World.Volume; i += sizeof(chunk)) {
		count = World.Volume - i; count = min(count, sizeof(chunk));

		for (j = 0; j < count; j++) { chunk[j] = beta_data[World.Blocks[i + j]]; }
		if ((res = Stream_Write(stream, chunk, count))) return res;
	}

	Mem_Copy(tmp, sc_data, sizeof(sc_data));
	{
		Stream_SetU32_BE(&tmp[7], World.Volume);
	}
	if ((res = Stream_Write(stream, tmp, sizeof(sc_data)))) return res;

	for (i = 0; i < World.Volume; i += sizeof(chunk)) {
		count = World.Volume - i; count = min(count, sizeof(chunk));

		for (j = 0; j < count; j++) { chunk[j] = beta_meta[World.Blocks[i + j]]; }
		if ((res = Stream_Write(stream, chunk, count))) return res;
	}

	return Stream_Write(stream, sc_end, sizeof(sc_end));
}

static void SaveMap(const String* path) {
	struct Stream stream, compStream;
	struct GZipState state;
	cc_result res;

	res = Stream_CreateFile(&stream, path);
	if (res) { Logger_Warn2(res, "creating", path); return; }
	GZip_MakeStream(&compStream, &state, &stream);

	res = SaveSchematic(&compStream);

	if (res) {
		stream.Close(&stream);
		Logger_Warn2(res, "encoding", path); return;
	}
		
	if ((res = compStream.Close(&compStream))) {
		stream.Close(&stream);
		Logger_Warn2(res, "closing", path); return;
	}

	res = stream.Close(&stream);
	if (res) { Logger_Warn2(res, "closing", path); return; }

	Chat_Add1("&eSaved map to: %s", path);
}



/*########################################################################################################################*
*---------------------------------------------------Plugin implementation-------------------------------------------------*
*#########################################################################################################################*/
#define SendChat(msg) const static String str = String_FromConst(msg); Chat_Add(&str);

static void SchematicExportCmd_Execute(const String* args, int argsCount) {
	if (!argsCount) { SendChat("&cFilename required."); return; }

	char strBuffer[FILENAME_SIZE];
	String str = String_FromArray(strBuffer);
	String_Format1(&str, "maps/%s.schematic", &args[0]);
	SaveMap(&str);

	str.length = 0;
	String_Format1(&str, "&eExported map to maps/%s.schematic", &args[0]);
	Chat_Add(&str);
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
	Commands_Register(&SchematicExportCmd);
}


/*########################################################################################################################*
*----------------------------------------------------Plugin boilerplate---------------------------------------------------*
*#########################################################################################################################*/
#ifdef CC_BUILD_WIN
// special attribute to get symbols exported with Visual Studio
#define PLUGIN_EXPORT __declspec(dllexport)
#else
// public symbols already exported when compiling shared lib with GCC
#define PLUGIN_EXPORT
#endif

PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = {
	SchematicExporter_Init /* Init */
};