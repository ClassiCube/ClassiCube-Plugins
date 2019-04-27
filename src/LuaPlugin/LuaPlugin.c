// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "../../../ClassicalSharp/src/GameStructs.h"
#include "../../../ClassicalSharp/src/Block.h"
#include "../../../ClassicalSharp/src/ExtMath.h"
#include "../../../ClassicalSharp/src/Game.h"
#include "../../../ClassicalSharp/src/Chat.h"
#include "../../../ClassicalSharp/src/Stream.h"
#include "../../../ClassicalSharp/src/TexturePack.h"
#include "../../../ClassicalSharp/src/World.h"
#include "../../../ClassicalSharp/src/Funcs.h"
#include "../../../ClassicalSharp/src/Event.h"

#ifdef _WIN64
#pragma comment(lib, "C:/GitPortable/Data/Home/ClassicalSharp/src/x64/Debug/ClassiCube.lib")
#else
#pragma comment(lib, "C:/GitPortable/Data/Home/ClassicalSharp/src/x86/Debug/ClassiCube.lib")
#endif

// ====== LUA BASE PLUGIN API ======
struct LuaPlugin;
typedef struct LuaPlugin { lua_State* L; struct LuaPlugin* next; } LuaPlugin;
static LuaPlugin* pluginsHead;

static void LuaPlugin_RaiseVoid(const char* groupName, const char* funcName) {
	LuaPlugin* plugin = pluginsHead;
	while (plugin) {
		lua_State* L = plugin->L;
		lua_getglobal(L, groupName);
		lua_getfield(L, -1, funcName);

		if (lua_isfunction(L, -1)) { 
			lua_call(L, 0, 0); /* call implicitly pops function */
		} else {
			lua_pop(L, 1); /* need to pop function name manually */
		}

		lua_pop(L, 1);
		plugin = plugin->next;
	}
}

// ====== LUA CHAT API ======
static int CC_Chat_Add(lua_State* L) {
	size_t len;
	const char* msg = lua_tolstring(L, -1, &len);

	String str = String_Init(msg, len, len);
	Chat_Add(&str);
	lua_pop(L, 1);
	return 0;
}

static const struct luaL_Reg chatFuncs[] = {
	{ "add", CC_Chat_Add },
	{ NULL, NULL }
};

// ====== LUA WORLD API ======
static int CC_World_GetDimensions(lua_State* L) {
	lua_pushinteger(L, World.Width);
	lua_pushinteger(L, World.Height);
	lua_pushinteger(L, World.Length);
	return 3;
}

static int CC_World_GetBlock(lua_State* L) {
	int x = lua_tointeger(L, -3);
	int y = lua_tointeger(L, -2);
	int z = lua_tointeger(L, -1);

	lua_pop(L, 3);
	lua_pushinteger(L, World_GetBlock(x, y, z));
	return 1;
}

static const struct luaL_Reg worldFuncs[] = {
	{ "getDimensions", CC_World_GetDimensions },
	{ "getBlock",      CC_World_GetBlock },
	{ NULL, NULL }
};

static void CC_World_OnNew(void* obj) {
	LuaPlugin_RaiseVoid("world", "onNewMap");
}
static void CC_World_OnMapLoaded(void* obj) {
	LuaPlugin_RaiseVoid("world", "onNewMapLoaded");
}

// ====== LUA PLUGIN ======
static void LuaPlugin_Register(lua_State* L) {
	luaL_newlib(L, chatFuncs);
	lua_setglobal(L, "chat");

	luaL_newlib(L, worldFuncs);
	lua_setglobal(L, "world");
	// TODO: move into LuaPlugin_New(L) (share with file/string
	// LuaPlugin_Load(L, filename, str) {
	// res = str ? loadfile(filename) : loadbuffer(str->buffer, 0, str->len)
}

static void LuaPlugin_Load(const String* origName, void* obj) {
	static String ext = String_FromConst(".lua");
	if (!String_CaselessEnds(origName, &ext)) return;
	String name; char nameBuffer[601];

	String_InitArray_NT(name, nameBuffer);
	String_Copy(&name, origName);
	name.buffer[name.length] = '\0';

	lua_State* L = luaL_newstate();
	luaL_openlibs(L);
	LuaPlugin_Register(L);

	//luaL_dofile(L, "test.lua");
	int res1 = luaL_loadfile(L, name.buffer);
	int res2 = lua_pcall(L, 0, LUA_MULTRET, 0);
	const char* msg = lua_tostring(L, -1);

	// no need to bother freeing
	LuaPlugin* plugin = Mem_Alloc(1, sizeof(LuaPlugin), "lua plugin");
	plugin->L    = L;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;
}

static void LuaPlugin_Init(void) {
	const static String luaDir = String_FromConst("lua");
	if (!Directory_Exists(&luaDir)) Directory_Create(&luaDir);
	Directory_Enum(&luaDir, NULL, LuaPlugin_Load);

	Event_RegisterVoid(&WorldEvents.NewMap,    NULL, CC_World_OnNew);
	Event_RegisterVoid(&WorldEvents.MapLoaded, NULL, CC_World_OnMapLoaded);
}

__declspec(dllexport) int Plugin_ApiVersion = 1;
__declspec(dllexport) struct IGameComponent Plugin_Component = {
	LuaPlugin_Init /* Init */
};
