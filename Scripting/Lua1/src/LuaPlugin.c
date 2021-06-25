// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "../../../../ClassicalSharp/src/String.h"

static cc_string LuaPlugin_GetString(lua_State* L, int idx) {
	size_t len;
	const char* msg = lua_tolstring(L, idx, &len);
	return String_Init(msg, len, len);
}

#define SCRIPTING_DIRECTORY "lua"
#define SCRIPTING_CONTEXT lua_State*
#define SCRIPTING_RESULT int

#define Scripting_GetString(ctx, arg) LuaPlugin_GetString(ctx, -(arg)-1)
#define Scripting_GetInt(ctx, arg) lua_tointeger(ctx, -(arg)-1)
#define Scripting_Consume(ctx, args) lua_pop(ctx, args)

#define Scripting_ReturnVoid(ctx) return 0;
#define Scripting_ReturnInt(ctx, value) lua_pushinteger(ctx, value); return 1;
#define Scripting_ReturnBoolean(ctx, value) lua_pushboolean(ctx, value); return 1;
#define Scripting_ReturnString(ctx, buffer, len) lua_pushlstring(ctx, buffer, len); return 1;

#include "Scripting.h"

// ====== LUA BASE PLUGIN API ======
struct LuaPlugin;
typedef struct LuaPlugin { lua_State* L; struct LuaPlugin* next; } LuaPlugin;
static LuaPlugin* pluginsHead;

static void LuaPlugin_LogError(lua_State* L, const char* place, const void* arg1, const void* arg2) {
	char buffer[256];
	cc_string str = String_FromArray(buffer);
	
	// kinda hacky and hardcoded but it works
	if (arg1 && arg2) {
		String_Format4(&str, "&cError %c (at %c.%c)", place, arg1, arg2, NULL);
	} else if (arg1) {
		String_Format4(&str, "&cError %c (%s)", place, arg1, NULL, NULL);
	} else {
		String_Format4(&str, "&cError", place, NULL, NULL, NULL);
	}

	Chat_Add(&str);
	str = LuaPlugin_GetString(L, -1);
	Chat_Add(&str);
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
		int ret = lua_pcall(L, 0, 0, 0); /* call implicitly pops function */
		if (ret) LuaPlugin_LogError(L, "running callback", groupName, funcName);
	LuaPlugin_RaiseCommonEnd
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
	LuaPlugin_RaiseCommonBegin
		lua_pushlstring(L, msg->buffer, msg->length);
		lua_pushinteger(L, msgType);
		int ret = lua_pcall(L, 2, 0, 0); /* call implicitly pops function */
		if (ret) LuaPlugin_LogError(L, "running callback", groupName, funcName);
	LuaPlugin_RaiseCommonEnd
}


/*########################################################################################################################*
*--------------------------------------------------------Chat api---------------------------------------------------------*
*#########################################################################################################################*/
static const struct luaL_Reg chatFuncs[] = {
	{ "add",  CC_Chat_Add },
	{ "send", CC_Chat_Send },
	{ NULL, NULL }
};


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static int CC_Server_SendData(lua_State* L) {
	cc_uint8* data;
	size_t len;
	int i, type = lua_type(L, -1);

	if (type == LUA_TSTRING) {
		data = lua_tolstring(L, -1, &len);
		Server.SendData(data, len);
	} else if (type == LUA_TTABLE) {
		len  = lua_rawlen(L, -1);
		data = Mem_Alloc(len, 1, "lua send data");

		for (i = 1; i <= len; i++) {
			lua_rawgeti(L, -1, i);
			data[i - 1] = lua_tointeger(L, -1);
			lua_pop(L, 1);
		}

		Server.SendData(data, len);
		Mem_Free(data);
	} else {
		luaL_error(L, "data must be string or an array table");
	}
	return 0;
}

static const struct luaL_Reg serverFuncs[] = {
	{ "getMotd",        CC_Server_GetMotd },
	{ "getName",        CC_Server_GetName },
	{ "getAppName",     CC_Server_GetAppName },
	{ "setAppName",     CC_Server_SetAppName },
	{ "isSingleplayer", CC_Server_IsSingleplayer },
	{ "sendData",       CC_Server_SendData },
	{ NULL, NULL }
};


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static const struct luaL_Reg worldFuncs[] = {
	{ "getWidth",  CC_World_GetWidth  },
	{ "getHeight", CC_World_GetHeight },
	{ "getLength", CC_World_GetLength },
	{ "getBlock",  CC_World_GetBlock  },
	{ NULL, NULL }
};


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static const struct luaL_Reg windowFuncs[] = {
	{ "setTitle", CC_Window_SetTitle },
	{ NULL, NULL }
};


/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static void LuaPlugin_Register(lua_State* L) {
	luaL_newlib(L, chatFuncs);   lua_setglobal(L, "chat");
	luaL_newlib(L, serverFuncs); lua_setglobal(L, "server");
	luaL_newlib(L, worldFuncs);  lua_setglobal(L, "world");
	luaL_newlib(L, windowFuncs); lua_setglobal(L, "window");
}

static lua_State* LuaPlugin_New(void) {
	lua_State* L = luaL_newstate();
	luaL_openlibs(L);
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
		LuaPlugin_LogError(L, "loading script", &name, NULL);
		lua_close(L); return;
	}

	res = lua_pcall(L, 0, LUA_MULTRET, 0);
	if (res) {
		LuaPlugin_LogError(L, "executing script", &name, NULL);
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
		LuaPlugin_LogError(L, "loading script", script, NULL);
	} else {
		res = lua_pcall(L, 0, LUA_MULTRET, 0);
		if (res) LuaPlugin_LogError(L, "executing script", script, NULL);
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
