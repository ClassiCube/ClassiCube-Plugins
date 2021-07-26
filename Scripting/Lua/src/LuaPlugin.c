#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

// LUA handles most of the complicated work already: */
//  resets stack before C function called - https://www.lua.org/pil/26.html */
//  clears stack after  C function returns - https://stackoverflow.com/questions/1217423/how-to-use-lua-pop-function-correctly */

#define SCRIPTING_DIRECTORY "lua"
#define SCRIPTING_ARGS lua_State* L
#define SCRIPTING_CALL L
#define SCRIPTING_RESULT int
#define SCRIPTING_FUNC const struct luaL_Reg
#define Scripting_DeclareFunc(name, func, num_args) { name, func }

#define Scripting_ReturnVoid() return 0;
#define Scripting_ReturnInt(value) lua_pushinteger(L, value); return 1;
#define Scripting_ReturnBool(value) lua_pushboolean(L, value); return 1;
#define Scripting_ReturnStr(buffer, len) lua_pushlstring(L, buffer, len); return 1;
#define Scripting_ReturnPtr(value) lua_pushlightuserdata(L, value); return 1;
#define Scripting_ReturnNum(value) lua_pushnumber(L, value); return 1;

#include "../../Scripting.h"
// Scripting_GetXYZ functions: don't forget to add 1 to arg, because LUA stack starts at 1

/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	cc_string str;
	size_t len;

	str.buffer   = lua_tolstring(L, arg + 1, &len);
	str.length   = len;
	str.capacity = 0;
	return str;
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	return (int)lua_tointeger(L, arg + 1);
}
static double Scripting_GetNum(SCRIPTING_ARGS, int arg) {
	return lua_tonumber(L, arg + 1);
}

static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {
	sc_buffer buffer = { 0 };
	size_t len;
	int i, idx = arg + 1, type = lua_type(L, idx);

	if (type == LUA_TSTRING) {
		buffer.data = lua_tolstring(L, idx, &len);
		buffer.len  = len;
	} else if (type == LUA_TTABLE) {
		buffer.len  = lua_rawlen(L, idx);
		buffer.data = Mem_Alloc(len, 1, "lua temp buffer");
		buffer.meta = 1;

		for (i = 1; i <= buffer.len; i++) {
			lua_rawgeti(L, idx, i);
			buffer.data[i - 1] = lua_tointeger(L, -1);
			lua_pop(L, 1);
		}
	} else {
		luaL_error(L, "data must be string or an array table");
	}
	return buffer;
}

static void Scripting_FreeStr(cc_string* str) {
	// no need to manually free
}
static void Scripting_FreeBuf(sc_buffer* buf) {
	// only need to free memory when data was a table
	if (buf->meta) Mem_Free(buf->data);
}


/*########################################################################################################################*
*--------------------------------------------------------Base API---------------------------------------------------------*
*#########################################################################################################################*/
struct LuaPlugin;
typedef struct LuaPlugin { lua_State* L; struct LuaPlugin* next; } LuaPlugin;
static LuaPlugin* pluginsHead;

static cc_string Backend_GetError(SCRIPTING_ARGS) {
	return Scripting_GetStr(L, -2); // really -1, but _GetStr adds 1
}

// macro to avoid verbose code duplication
#define LuaPlugin_RaiseCommonBegin \
	LuaPlugin* plugin = pluginsHead;\
	while (plugin) {\
		lua_State* L = plugin->L;\
		lua_getglobal(L, groupName);\
		lua_getfield(L, -1, funcName);\
		if (lua_isfunction(L, -1)) { \

#define LuaPlugin_RaiseCommonEnd \
		} else {\
			lua_pop(L, 1); /* pop field manually */ \
		}\
		lua_pop(L, 1); /* pop table name */ \
		plugin = plugin->next;\
	}

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
	LuaPlugin_RaiseCommonBegin
		int ret = lua_pcall(L, 0, 0, 0); // call implicitly pops function
		if (ret) Scripting_LogError(L, "running callback", groupName, funcName);
	LuaPlugin_RaiseCommonEnd
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
	LuaPlugin_RaiseCommonBegin
		lua_pushlstring(L, msg->buffer, msg->length);
		lua_pushinteger(L, msgType);
		int ret = lua_pcall(L, 2, 0, 0); // call implicitly pops function
		if (ret) Scripting_LogError(L, "running callback", groupName, funcName);
	LuaPlugin_RaiseCommonEnd
}


/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
// LUA core libraries, see linit.c for more details
//  io/os libraries are removed for security, remaining libaries are left unaltered
static const luaL_Reg loadedlibs[] = {
	{ "_G", luaopen_base },
	{ LUA_LOADLIBNAME, luaopen_package },
	{ LUA_COLIBNAME,   luaopen_coroutine },
	{ LUA_TABLIBNAME,  luaopen_table },
	{ LUA_STRLIBNAME,  luaopen_string },
	{ LUA_MATHLIBNAME, luaopen_math },
	{ LUA_UTF8LIBNAME, luaopen_utf8 },
	{ LUA_DBLIBNAME,   luaopen_debug },
	{ NULL, NULL }
};
static void LuaPlugin_RegisterCore(lua_State* L) {
	const luaL_Reg *lib;
	// "require" functions from 'loadedlibs' and set results to global table
	for (lib = loadedlibs; lib->func; lib++) {
		luaL_requiref(L, lib->name, lib->func, 1);
		lua_pop(L, 1); // remove lib
	}
}

static void LuaPlugin_Register(lua_State* L) {
	luaL_newlib(L, blockFuncs);     lua_setglobal(L, "block");
	luaL_newlib(L, cameraFuncs);    lua_setglobal(L, "camera");
	luaL_newlib(L, chatFuncs);      lua_setglobal(L, "chat");
	luaL_newlib(L, envFuncs);       lua_setglobal(L, "env");
	luaL_newlib(L, gameFuncs);      lua_setglobal(L, "game");
	luaL_newlib(L, inventoryFuncs); lua_setglobal(L, "inventory");
	luaL_newlib(L, playerFuncs);    lua_setglobal(L, "player");
	luaL_newlib(L, serverFuncs);    lua_setglobal(L, "server");
	luaL_newlib(L, tablistFuncs);   lua_setglobal(L, "tablist");
	luaL_newlib(L, worldFuncs);     lua_setglobal(L, "world");
	luaL_newlib(L, windowFuncs);    lua_setglobal(L, "window");
}

static lua_State* LuaPlugin_New(void) {
	lua_State* L = luaL_newstate();
	LuaPlugin_RegisterCore(L);
	LuaPlugin_Register(L);
	return L;
}

static void Backend_Load(const cc_string* origName, void* obj) {
	static cc_string ext = String_FromConst(".lua");
	if (!String_CaselessEnds(origName, &ext)) return;
	cc_string name; char nameBuffer[601];
	int res;

	String_InitArray_NT(name, nameBuffer);
	String_Copy(&name, origName);
	name.buffer[name.length] = '\0';

	lua_State* L = LuaPlugin_New();
	//luaL_dofile(L, "test.lua");
	res = luaL_loadfile(L, name.buffer);
	if (res) {
		Scripting_LogError(L, "loading script", &name, NULL);
		lua_close(L); return;
	}

	res = lua_pcall(L, 0, LUA_MULTRET, 0);
	if (res) {
		Scripting_LogError(L, "executing script", &name, NULL);
		lua_close(L); return;
	}	

	// no need to bother freeing
	LuaPlugin* plugin = Mem_Alloc(1, sizeof(LuaPlugin), "lua plugin");
	plugin->L    = L;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;
}

static void Backend_ExecScript(const cc_string* script) {
	lua_State* L  = LuaPlugin_New();
	cc_result res = luaL_loadbuffer(L, script->buffer, script->length, "@tmp");
	if (res) {
		Scripting_LogError(L, "loading script", script, NULL);
	} else {
		res = lua_pcall(L, 0, LUA_MULTRET, 0);
		if (res) Scripting_LogError(L, "executing script", script, NULL);
	}
	lua_close(L);
}

static struct ChatCommand LuaPlugin_Cmd = {
	"lua", Scripting_Handle, false,
	{
		"&a/client lua [script]",
		"&eExecutes the input text as a lua script",
	}
};

static void Backend_Init(void) {
	Commands_Register(&LuaPlugin_Cmd);
}
