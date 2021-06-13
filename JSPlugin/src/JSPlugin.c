// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

#include "duktape.h"

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "../../../ClassicalSharp/src/Game.h"
#include "../../../ClassicalSharp/src/Block.h"
#include "../../../ClassicalSharp/src/ExtMath.h"
#include "../../../ClassicalSharp/src/Chat.h"
#include "../../../ClassicalSharp/src/Stream.h"
#include "../../../ClassicalSharp/src/TexturePack.h"
#include "../../../ClassicalSharp/src/World.h"
#include "../../../ClassicalSharp/src/Funcs.h"
#include "../../../ClassicalSharp/src/Event.h"
#include "../../../ClassicalSharp/src/Server.h"
#include "../../../ClassicalSharp/src/String.h"
#include "../../../ClassicalSharp/src/Window.h"

#ifdef _WIN64
#pragma comment(lib, "C:/GitPortable/Data/Home/ClassicalSharp/src/x64/Debug/ClassiCube.lib")
#else
#pragma comment(lib, "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/x86/Debug/ClassiCube.lib")
#endif

// ====== LUA BASE PLUGIN API ======
struct JSPlugin;
typedef struct JSPlugin { duk_context* ctx; struct JSPlugin* next; } JSPlugin;
static JSPlugin* pluginsHead;

static cc_string JSPlugin_GetString(duk_context* ctx, int idx) {
	duk_size_t len;
	const char* msg = duk_to_lstring(ctx, idx, &len);
	return String_Init(msg, len, len);
}

static void JSPlugin_LogError(duk_context* ctx, const char* place, const void* arg1, const void* arg2) {
	char buffer[256];
	cc_string str = String_FromArray(buffer);
	
	// kinda hacky and hardcoded but it works
	if (arg1 && arg2) {
		String_Format4(&str, "&cError %c (at %c.%c)", place, arg1, arg2, NULL);
	} else if (arg1) {
		String_Format4(&str, "&cError %c (%s)", place, arg1, NULL, NULL);
	} else {
		String_Format4(&str, "&cError %c", place, NULL, NULL, NULL);
	}
	Chat_Add(&str);

	str = String_FromReadonly(duk_safe_to_string(ctx, -1));
	Chat_Add(&str);
}


// macro to avoid verbose code duplication
#define JSPlugin_RaiseCommonBegin \
	JSPlugin* plugin = pluginsHead;\
	while (plugin) {\
		duk_context* ctx = plugin->ctx;\
		duk_push_global_object(ctx);\
		duk_get_prop_string(ctx, -1, groupName);\
		duk_get_prop_string(ctx, -1, funcName);\
		if (duk_is_function(ctx, -1)) { \

#define JSPlugin_RaiseCommonEnd \
		} else {\
			duk_pop_n(ctx, 1); /* pop function manually */ \
		}\
		duk_pop_n(ctx, 2); /* pop object name and global */ \
		plugin = plugin->next;\
	}

static void JSPlugin_RaiseVoid(const char* groupName, const char* funcName) {
	JSPlugin_RaiseCommonBegin
		duk_int_t ret = duk_pcall(ctx, 0);
		if (ret != 0) JSPlugin_LogError(ctx, "running callback", groupName, funcName);
	JSPlugin_RaiseCommonEnd
}

static void JSPlugin_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
	JSPlugin_RaiseCommonBegin
		duk_push_lstring(ctx, msg->buffer, msg->length);
		duk_push_int(ctx, msgType);
		duk_int_t ret = duk_pcall(ctx, 2);
		if (ret != 0) JSPlugin_LogError(ctx, "running callback", groupName, funcName);
	JSPlugin_RaiseCommonEnd
}


/*########################################################################################################################*
*--------------------------------------------------------Chat api---------------------------------------------------------*
*#########################################################################################################################*/
static duk_ret_t CC_Chat_Add(duk_context* ctx) {
	cc_string str = JSPlugin_GetString(ctx, -1);
	Chat_Add(&str);
	return 1;
}

static duk_ret_t CC_Chat_Send(duk_context* ctx) {
	cc_string str = JSPlugin_GetString(ctx, -1);
	Chat_Send(&str, false);
	return 1;
}

static const duk_function_list_entry chatFuncs[] = {
	{ "add",  CC_Chat_Add, 1 },
	{ "send", CC_Chat_Send, 1 },
	{ NULL, NULL, 0}
};

static void CC_Chat_OnReceived(void* obj, const cc_string* msg, int msgType) {
	JSPlugin_RaiseChat("chat", "onReceived", msg, msgType);
}
static void CC_Chat_OnSent(void* obj, const cc_string* msg, int msgType) {
	JSPlugin_RaiseChat("chat", "onSent", msg, msgType);
}
static void CC_Chat_Hook(void) {
	Event_Register_(&ChatEvents.ChatReceived, NULL, CC_Chat_OnReceived);
	Event_Register_(&ChatEvents.ChatSending,  NULL, CC_Chat_OnSent);
}


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static duk_ret_t CC_Server_GetMotd(duk_context* ctx) {
	duk_push_lstring(ctx, Server.MOTD.buffer, Server.MOTD.length);
	return 1;
}
static duk_ret_t CC_Server_GetName(duk_context* ctx) {
	duk_push_lstring(ctx, Server.Name.buffer, Server.Name.length);
	return 1;
}
static duk_ret_t CC_Server_GetAppName(duk_context* ctx) {
	duk_push_lstring(ctx, Server.AppName.buffer, Server.AppName.length);
	return 1;
}

static duk_ret_t CC_Server_SetAppName(duk_context* ctx) {
	cc_string str = JSPlugin_GetString(ctx, -1);
	String_Copy(&Server.AppName, &str);
	return 1;
}

static duk_ret_t CC_Server_IsSingleplayer(duk_context* ctx) {
	duk_push_boolean(ctx, Server.IsSinglePlayer);
	return 1;
}

static duk_ret_t CC_Server_SendData(duk_context* ctx) {
	duk_size_t size;
	void* data = duk_to_buffer(ctx, -1, &size);
	Server.SendData(data, size);
	return 1;
}

static const duk_function_list_entry serverFuncs[] = {
	{ "getMotd",        CC_Server_GetMotd, 0 },
	{ "getName",        CC_Server_GetName, 0 },
	{ "getAppName",     CC_Server_GetAppName, 0 },
	{ "setAppName",     CC_Server_SetAppName, 1 },
	{ "isSingleplayer", CC_Server_IsSingleplayer, 0 },
	{ "sendData",       CC_Server_SendData, 1},
	{ NULL, NULL, 0 }
};

static void CC_Server_OnConnected(void* obj) {
	JSPlugin_RaiseVoid("server", "onConnected");
}
static void CC_Server_OnDisconnected(void* obj) {
	JSPlugin_RaiseVoid("server", "onDisconnected");
}
static void CC_Server_Hook(void) {
	Event_Register_(&NetEvents.Connected,    NULL, CC_Server_OnConnected);
	Event_Register_(&NetEvents.Disconnected, NULL, CC_Server_OnDisconnected);
}


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static duk_ret_t CC_World_GetWidth(duk_context* ctx) {
	duk_push_int(ctx, World.Width);
	return 1;
}

static duk_ret_t CC_World_GetHeight(duk_context* ctx) {
	duk_push_int(ctx, World.Height);
	return 1;
}

static duk_ret_t CC_World_GetLength(duk_context* ctx) {
	duk_push_int(ctx, World.Height);
	return 1;
}

static duk_ret_t CC_World_GetBlock(duk_context* ctx) {
	duk_int_t x = duk_to_int(ctx, -3);
	duk_int_t y = duk_to_int(ctx, -2);
	duk_int_t z = duk_to_int(ctx, -1);

	duk_push_int(ctx, World_GetBlock(x, y, z));
	return 1;
}

static const duk_function_list_entry worldFuncs[] = {
	{ "getWidth",  CC_World_GetWidth, 0, },
	{ "getHeight", CC_World_GetHeight, 0 },
	{ "getLength", CC_World_GetLength, 0 },
	{ "getBlock",  CC_World_GetBlock, 3 },
	{ NULL, NULL, 0 }
};

static void CC_World_OnNew(void* obj) {
	JSPlugin_RaiseVoid("world", "onNewMap");
}
static void CC_World_OnMapLoaded(void* obj) {
	JSPlugin_RaiseVoid("world", "onMapLoaded");
}
static void CC_World_Hook(void) {
	Event_Register_(&WorldEvents.NewMap,    NULL, CC_World_OnNew);
	Event_Register_(&WorldEvents.MapLoaded, NULL, CC_World_OnMapLoaded);
}


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static duk_ret_t CC_Window_SetTitle(duk_context* ctx) {
	cc_string str = JSPlugin_GetString(ctx, -1);
	Window_SetTitle(&str);
	return 1;
}

static const duk_function_list_entry windowFuncs[] = {
	{ "setTitle", CC_Window_SetTitle, 1 },
	{ NULL, NULL, 0 }
};


/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static void JSPlugin_RegisterModule(duk_context* ctx, const char* name, const duk_function_list_entry* funcs) {
	duk_push_global_object(ctx);
	duk_push_object(ctx);
	duk_put_function_list(ctx, -1, funcs);
	duk_put_prop_string(ctx, -2, name);
	duk_pop(ctx);
}

static void JSPlugin_Register(duk_context* ctx) {
	JSPlugin_RegisterModule(ctx, "chat",   chatFuncs);
	JSPlugin_RegisterModule(ctx, "server", serverFuncs);
	JSPlugin_RegisterModule(ctx, "world",  worldFuncs);
	JSPlugin_RegisterModule(ctx, "window", windowFuncs);
}

static duk_context* JSPlugin_New(void) {
	duk_context* ctx = duk_create_heap_default();
	JSPlugin_Register(ctx);
	return ctx;
}

static cc_result JSPlugin_LoadFile(duk_context* ctx, const cc_string* path) {
	/* TODO: What's error checking anyways? */
	struct Stream s;
	cc_result res;
	if ((res = Stream_OpenFile(&s, path))) return res;

	cc_uint32 length;
	s.Length(&s, &length);

	void* data = Mem_Alloc(length, 1, "JS file");
	Stream_Read(&s, data, length);
	duk_push_lstring(ctx, data, length);

	s.Close(&s);
	return 0;
}

static void JSPlugin_Load(const cc_string* path, void* obj) {
	static cc_string ext = String_FromConst(".lua");
	if (!String_CaselessEnds(path, &ext)) return;
	int res;

	duk_context* ctx = JSPlugin_New();
	res = JSPlugin_LoadFile(ctx, path);
	if (res) {
		duk_destroy_heap(ctx); return;
	}

	if (duk_peval(ctx) != 0) {
		JSPlugin_LogError(ctx, "executing script", path, NULL);
		duk_destroy_heap(ctx); return;
	}	

	// no need to bother freeing
	JSPlugin* plugin = Mem_Alloc(1, sizeof(JSPlugin), "lua plugin");
	plugin->ctx  = ctx;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;
}

static void JSPlugin_ExecCmd(const cc_string* args, int argsCount) {
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

	duk_context* ctx  = JSPlugin_New();
	duk_push_lstring(ctx, tmp.buffer, tmp.length);

	if (duk_peval(ctx) != 0) {
		JSPlugin_LogError(ctx, "loading script", &tmp, NULL);
	}
	/* TODO: Compile and call */
	duk_destroy_heap(ctx);
}

static struct ChatCommand JSPlugin_Cmd = {
	"js", JSPlugin_ExecCmd, false,
	{
		"&a/client js [script]",
		"&eExecutes the input text as a JavaScript script",
	}
};

static void JSPlugin_Init(void) {
	const static cc_string jsDir = String_FromConst("js");
	Directory_Create(&jsDir);
	Directory_Enum(&jsDir, NULL, JSPlugin_Load);
	Commands_Register(&JSPlugin_Cmd);

	CC_Chat_Hook();
	CC_Server_Hook();
	CC_World_Hook();
}

__declspec(dllexport) int Plugin_ApiVersion = 1;
__declspec(dllexport) struct IGameComponent Plugin_Component = {
	JSPlugin_Init /* Init */
};
