// NOTE: Make sure the Scripting.h files in the various backend folders
//  are kept up to date with the Scripting.h file in the root folder
// ====================================================================

// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "../../../../ClassicalSharp/src/Game.h"
#include "../../../../ClassicalSharp/src/String.h"
#include "../../../../ClassicalSharp/src/Block.h"
#include "../../../../ClassicalSharp/src/ExtMath.h"
#include "../../../../ClassicalSharp/src/Chat.h"
#include "../../../../ClassicalSharp/src/Stream.h"
#include "../../../../ClassicalSharp/src/TexturePack.h"
#include "../../../../ClassicalSharp/src/World.h"
#include "../../../../ClassicalSharp/src/Funcs.h"
#include "../../../../ClassicalSharp/src/Event.h"
#include "../../../../ClassicalSharp/src/Server.h"
#include "../../../../ClassicalSharp/src/Window.h"

#ifdef _WIN64
#pragma comment(lib, "../../../../ClassicalSharp/src/x64/Debug/ClassiCube.lib")
#else
#pragma comment(lib, "../../../../ClassicalSharp/src/x86/Debug/ClassiCube.lib")
#endif

static void Backend_RaiseVoid(const char* groupName, const char* funcName);
static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType);

static void Backend_Load(const cc_string* origName, void* obj);
static void Backend_ExecScript(const cc_string* script);
static void Backend_Init(void);

/*
#define SCRIPTING_DIRECTORY
#define SCRIPTING_CONTEXT

#define Scripting_GetString(SCRIPTING_CONTEXT, arg)
#define Scripting_GetInt(SCRIPTING_CONTEXT, arg)
#define Scripting_Consume(SCRIPTING_CONTEXT, args)

#define SCRIPTING_RESULT
#define Scripting_ReturnVoid(SCRIPTING_CONTEXT)
#define Scripting_ReturnInt(SCRIPTING_CONTEXT, value)
#define Scripting_ReturnBoolean(SCRIPTING_CONTEXT, value)
#define Scripting_ReturnString(SCRIPTING_CONTEXT, buffer, len)
*/


/*########################################################################################################################*
*--------------------------------------------------------Chat api---------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Chat_Add(SCRIPTING_CONTEXT ctx) {
	cc_string str = Scripting_GetString(ctx, 0);
	Chat_Add(&str);

	Scripting_Consume(ctx, 1);
	Scripting_ReturnVoid(ctx);
}

static SCRIPTING_RESULT CC_Chat_Send(SCRIPTING_CONTEXT ctx) {
	cc_string str = Scripting_GetString(ctx, 0);
	Chat_Send(&str, false);

	Scripting_Consume(ctx, 1);
	Scripting_ReturnVoid(ctx);
}

static void CC_Chat_OnReceived(void* obj, const cc_string* msg, int msgType) {
	Backend_RaiseChat("chat", "onReceived", msg, msgType);
}
static void CC_Chat_OnSent(void* obj, const cc_string* msg, int msgType) {
	Backend_RaiseChat("chat", "onSent", msg, msgType);
}
static void CC_Chat_Hook(void) {
	Event_Register_(&ChatEvents.ChatReceived, NULL, CC_Chat_OnReceived);
	Event_Register_(&ChatEvents.ChatSending,  NULL, CC_Chat_OnSent);
}


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Server_GetMotd(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnString(ctx, Server.MOTD.buffer, Server.MOTD.length);
}
static SCRIPTING_RESULT CC_Server_GetName(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnString(ctx, Server.Name.buffer, Server.Name.length);
}
static SCRIPTING_RESULT CC_Server_GetAppName(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnString(ctx, Server.AppName.buffer, Server.AppName.length);
}

static SCRIPTING_RESULT CC_Server_SetAppName(SCRIPTING_CONTEXT ctx) {
	cc_string str = Scripting_GetString(ctx, 0);
	String_Copy(&Server.AppName, &str);

	Scripting_Consume(ctx, 1);
	Scripting_ReturnVoid(ctx);
}

static SCRIPTING_RESULT CC_Server_IsSingleplayer(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnBoolean(ctx, Server.IsSinglePlayer);
}

/* this one is too tricky to abstract */
/*static SCRIPTING_RESULT CC_Server_SendData(lua_State* L) {
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
}*/

static void CC_Server_OnConnected(void* obj) {
	Backend_RaiseVoid("server", "onConnected");
}
static void CC_Server_OnDisconnected(void* obj) {
	Backend_RaiseVoid("server", "onDisconnected");
}
static void CC_Server_Hook(void) {
	Event_Register_(&NetEvents.Connected,    NULL, CC_Server_OnConnected);
	Event_Register_(&NetEvents.Disconnected, NULL, CC_Server_OnDisconnected);
}


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_World_GetWidth(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnInt(ctx, World.Width);
}

static SCRIPTING_RESULT CC_World_GetHeight(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnInt(ctx, World.Height);
}

static SCRIPTING_RESULT CC_World_GetLength(SCRIPTING_CONTEXT ctx) {
	Scripting_ReturnInt(ctx, World.Length);
}

static SCRIPTING_RESULT CC_World_GetBlock(SCRIPTING_CONTEXT ctx) {
	int x = Scripting_GetInt(ctx, 2);
	int y = Scripting_GetInt(ctx, 1);
	int z = Scripting_GetInt(ctx, 0);

	Scripting_Consume(ctx, 3);
	Scripting_ReturnInt(ctx, World_GetBlock(x, y, z));
}

static void CC_World_OnNew(void* obj) {
	Backend_RaiseVoid("world", "onNewMap");
}
static void CC_World_OnMapLoaded(void* obj) {
	Backend_RaiseVoid("world", "onMapLoaded");
}
static void CC_World_Hook(void) {
	Event_Register_(&WorldEvents.NewMap,    NULL, CC_World_OnNew);
	Event_Register_(&WorldEvents.MapLoaded, NULL, CC_World_OnMapLoaded);
}


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Window_SetTitle(SCRIPTING_CONTEXT ctx) {
	cc_string str = Scripting_GetString(ctx, 0);
	Window_SetTitle(&str);

	Scripting_Consume(ctx, 1);
	Scripting_ReturnVoid(ctx);
}


/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static void Scripting_Init(void) {
	const static cc_string dir = String_FromConst(SCRIPTING_DIRECTORY);
	Directory_Create(&dir);

	Backend_Init();
	Directory_Enum(&dir, NULL, Backend_Load);

	CC_Chat_Hook();
	CC_Server_Hook();
	CC_World_Hook();
}

static void Scripting_Handle(const cc_string* args, int argsCount) {
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
	Backend_ExecScript(&tmp);
}

#ifdef CC_BUILD_WIN
// special attribute to get symbols exported on Windows
#define PLUGIN_EXPORT __declspec(dllexport)
#else
// public symbols already exported when compiling shared lib with GCC
#define PLUGIN_EXPORT
#endif

PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = {
	Scripting_Init /* Init */
};
