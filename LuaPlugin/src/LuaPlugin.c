// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "../../../ClassicalSharp/src/Game.h"
#include "../../../ClassicalSharp/src/String.h"
#include "../../../ClassicalSharp/src/Block.h"
#include "../../../ClassicalSharp/src/ExtMath.h"
#include "../../../ClassicalSharp/src/Chat.h"
#include "../../../ClassicalSharp/src/Stream.h"
#include "../../../ClassicalSharp/src/TexturePack.h"
#include "../../../ClassicalSharp/src/World.h"
#include "../../../ClassicalSharp/src/Funcs.h"
#include "../../../ClassicalSharp/src/Event.h"
#include "../../../ClassicalSharp/src/Server.h"
#include "../../../ClassicalSharp/src/Window.h"

#ifdef _WIN64
#pragma comment(lib, "C:/GitPortable/Data/Home/ClassicalSharp/src/x64/Debug/ClassiCube.lib")
#else
#pragma comment(lib, "C:/GitPortable/Data/Home/ClassicalSharp/src/x86/Debug/ClassiCube.lib")
#endif

// ====== LUA BASE PLUGIN API ======
struct LuaPlugin;
typedef struct LuaPlugin { lua_State* L; struct LuaPlugin* next; } LuaPlugin;
static LuaPlugin* pluginsHead;

static cc_string LuaPlugin_GetString(lua_State* L, int idx) {
	size_t len;
	const char* msg = lua_tolstring(L, idx, &len);
	return String_Init(msg, len, len);
}

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

static void LuaPlugin_RaiseVoid(const char* groupName, const char* funcName) {
	LuaPlugin_RaiseCommonBegin
		int ret = lua_pcall(L, 0, 0, 0); /* call implicitly pops function */
		if (ret) LuaPlugin_LogError(L, "running callback", groupName, funcName);
	LuaPlugin_RaiseCommonEnd
}

static void LuaPlugin_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
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
static int CC_Chat_Add(lua_State* L) {
	cc_string str = LuaPlugin_GetString(L, -1);
	Chat_Add(&str);
	lua_pop(L, 1);
	return 0;
}

static int CC_Chat_Send(lua_State* L) {
	cc_string str = LuaPlugin_GetString(L, -1);
	Chat_Send(&str, false);
	lua_pop(L, 1);
	return 0;
}

static const struct luaL_Reg chatFuncs[] = {
	{ "add",  CC_Chat_Add },
	{ "send", CC_Chat_Send },
	{ NULL, NULL }
};

static void CC_Chat_OnReceived(void* obj, const cc_string* msg, int msgType) {
	LuaPlugin_RaiseChat("chat", "onReceived", msg, msgType);
}
static void CC_Chat_OnSent(void* obj, const cc_string* msg, int msgType) {
	LuaPlugin_RaiseChat("chat", "onSent", msg, msgType);
}
static void CC_Chat_Hook(void) {
	Event_Register_(&ChatEvents.ChatReceived, NULL, CC_Chat_OnReceived);
	Event_Register_(&ChatEvents.ChatSending,  NULL, CC_Chat_OnSent);
}


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static int CC_Server_GetMotd(lua_State* L) {
	lua_pushlstring(L, Server.MOTD.buffer, Server.MOTD.length);
	return 1;
}
static int CC_Server_GetName(lua_State* L) {
	lua_pushlstring(L, Server.Name.buffer, Server.Name.length);
	return 1;
}
static int CC_Server_GetAppName(lua_State* L) {
	lua_pushlstring(L, Server.AppName.buffer, Server.AppName.length);
	return 1;
}

static int CC_Server_SetAppName(lua_State* L) {
	cc_string str = LuaPlugin_GetString(L, -1);
	String_Copy(&Server.AppName, &str);
	lua_pop(L, 1);
	return 0;
}

static int CC_Server_IsSingleplayer(lua_State* L) {
	lua_pushboolean(L, Server.IsSinglePlayer);
	return 1;
}

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

static void CC_Server_OnConnected(void* obj) {
	LuaPlugin_RaiseVoid("server", "onConnected");
}
static void CC_Server_OnDisconnected(void* obj) {
	LuaPlugin_RaiseVoid("server", "onDisconnected");
}
static void CC_Server_Hook(void) {
	Event_Register_(&NetEvents.Connected,    NULL, CC_Server_OnConnected);
	Event_Register_(&NetEvents.Disconnected, NULL, CC_Server_OnDisconnected);
}


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static int CC_World_GetWidth(lua_State* L) {
	lua_pushinteger(L, World.Width);
	return 1;
}
static int CC_World_GetHeight(lua_State* L) {
	lua_pushinteger(L, World.Height);
	return 1;
}
static int CC_World_GetLength(lua_State* L) {
	lua_pushinteger(L, World.Length);
	return 1;
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
	{ "getWidth",  CC_World_GetWidth  },
	{ "getHeight", CC_World_GetHeight },
	{ "getLength", CC_World_GetLength },
	{ "getBlock",  CC_World_GetBlock  },
	{ NULL, NULL }
};

static void CC_World_OnNew(void* obj) {
	LuaPlugin_RaiseVoid("world", "onNewMap");
}
static void CC_World_OnMapLoaded(void* obj) {
	LuaPlugin_RaiseVoid("world", "onMapLoaded");
}
static void CC_World_Hook(void) {
	Event_Register_(&WorldEvents.NewMap,    NULL, CC_World_OnNew);
	Event_Register_(&WorldEvents.MapLoaded, NULL, CC_World_OnMapLoaded);
}


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static int CC_Window_SetTitle(lua_State* L) {
	cc_string str = LuaPlugin_GetString(L, -1);
	Window_SetTitle(&str);
	lua_pop(L, 1);
	return 0;
}

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

static void LuaPlugin_Load(const cc_string* origName, void* obj) {
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

static void LuaPlugin_ExecCmd(const cc_string* args, int argsCount) {
	if (argsCount == 0) {
		Chat_Add(&(const cc_string)String_FromConst("&cNot enough arguments. See help"));
		return;
	}

	char buffer[1024];
	cc_string tmp = String_FromArray(buffer);
	for (int i = 0; i < argsCount; i++) {
		String_AppendString(&tmp, &args[i]);
		String_Append(&tmp, ' ');
	}

	lua_State* L  = LuaPlugin_New();
	cc_result res = luaL_loadbuffer(L, tmp.buffer, tmp.length, "@tmp");
	if (res) {
		LuaPlugin_LogError(L, "loading script", &tmp, NULL);
	} else {
		res = lua_pcall(L, 0, LUA_MULTRET, 0);
		if (res) LuaPlugin_LogError(L, "executing script", &tmp, NULL);
	}
	lua_close(L);
}

static struct ChatCommand LuaPlugin_Cmd = {
	"lua", LuaPlugin_ExecCmd, false,
	{
		"&a/client lua [script]",
		"&eExecutes the input text as a lua script",
	}
};

static void LuaPlugin_Init(void) {
	const static cc_string luaDir = String_FromConst("lua");
	Directory_Create(&luaDir);

	Directory_Enum(&luaDir, NULL, LuaPlugin_Load);
	Commands_Register(&LuaPlugin_Cmd);

	CC_Chat_Hook();
	CC_Server_Hook();
	CC_World_Hook();
}

__declspec(dllexport) int Plugin_ApiVersion = 1;
__declspec(dllexport) struct IGameComponent Plugin_Component = {
	LuaPlugin_Init /* Init */
};
