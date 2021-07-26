#include "duktape.h"

#define SCRIPTING_DIRECTORY "js"
#define SCRIPTING_ARGS duk_context* ctx
#define SCRIPTING_CALL ctx
#define SCRIPTING_RESULT duk_ret_t
#define SCRIPTING_FUNC const duk_function_list_entry
#define Scripting_DeclareFunc(name, func, num_args) { name, func, num_args }

#define Scripting_ReturnVoid() return 1;
#define Scripting_ReturnInt(value) duk_push_int(ctx, value); return 1;
#define Scripting_ReturnBool(value) duk_push_boolean(ctx, value); return 1;
#define Scripting_ReturnStr(buffer, len) duk_push_lstring(ctx, buffer, len); return 1;
#define Scripting_ReturnPtr(value) duk_push_pointer(ctx, value); return 1;
#define Scripting_ReturnNum(value) duk_push_number(ctx, value); return 1;

#include "../../Scripting.h"

/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	cc_string str;
	duk_size_t len;

	str.buffer   = duk_to_lstring(ctx, arg, &len);
	str.length   = len;
	str.capacity = 0;
	return str;
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	return duk_to_int(ctx, arg);
}
static double Scripting_GetNum(SCRIPTING_ARGS, int arg) {
	return duk_to_number(ctx, arg);
}

static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {
	sc_buffer buffer;
	duk_size_t size;

	buffer.data = duk_to_buffer(ctx, arg, &size);
	buffer.len  = size;
	return buffer;
}

static void Scripting_FreeStr(cc_string* str) {
	// no need to manually free
}
static void Scripting_FreeBuf(sc_buffer* buf) {
	// no need to manually free
}


/*########################################################################################################################*
*--------------------------------------------------------Base API---------------------------------------------------------*
*#########################################################################################################################*/
struct JSPlugin;
typedef struct JSPlugin { duk_context* ctx; struct JSPlugin* next; } JSPlugin;
static JSPlugin* pluginsHead;

static cc_string Backend_GetError(SCRIPTING_ARGS) {
	return String_FromReadonly(duk_safe_to_string(ctx, -1));
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

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
	JSPlugin_RaiseCommonBegin
		duk_int_t ret = duk_pcall(ctx, 0);
		if (ret != 0) JSPlugin_LogError(ctx, "running callback", groupName, funcName);
	JSPlugin_RaiseCommonEnd
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
	JSPlugin_RaiseCommonBegin
		duk_push_lstring(ctx, msg->buffer, msg->length);
		duk_push_int(ctx, msgType);
		duk_int_t ret = duk_pcall(ctx, 2);
		if (ret != 0) JSPlugin_LogError(ctx, "running callback", groupName, funcName);
	JSPlugin_RaiseCommonEnd
}


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
	JSPlugin_RegisterModule(ctx, "block",     blockFuncs);
	JSPlugin_RegisterModule(ctx, "camera",    cameraFuncs);
	JSPlugin_RegisterModule(ctx, "env",       envFuncs);
	JSPlugin_RegisterModule(ctx, "chat",      chatFuncs);
	JSPlugin_RegisterModule(ctx, "game",      gameFuncs);
	JSPlugin_RegisterModule(ctx, "inventory", inventoryFuncs);
	JSPlugin_RegisterModule(ctx, "player",    playerFuncs);
	JSPlugin_RegisterModule(ctx, "server",    serverFuncs);
	JSPlugin_RegisterModule(ctx, "tablist",   tablistFuncs);
	JSPlugin_RegisterModule(ctx, "world",     worldFuncs);
	JSPlugin_RegisterModule(ctx, "window",    windowFuncs);
}

static duk_context* JSPlugin_New(void) {
	duk_context* ctx = duk_create_heap_default();
	JSPlugin_Register(ctx);
	return ctx;
}

static void Backend_Load(const cc_string* path, void* obj) {
	static cc_string ext = String_FromConst(".js");
	if (!String_CaselessEnds(path, &ext)) return;
	cc_result res;

	sc_buffer mem;
	// TODO: What's error checking anyways?
	// TODO: leaking memory here
	res = Scripting_LoadFile(path, &mem);
	if (res) return;

	duk_context* ctx = JSPlugin_New();
	duk_push_lstring(ctx, mem.data, mem.len);

	if (duk_peval(ctx) != 0) {
		JSPlugin_LogError(ctx, "executing script", path, NULL);
		duk_destroy_heap(ctx); return;
	}	

	// no need to bother freeing
	JSPlugin* plugin = Mem_Alloc(1, sizeof(JSPlugin), "js plugin");
	plugin->ctx  = ctx;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;
}

static void Backend_ExecScript(const cc_string* script) {
	duk_context* ctx  = JSPlugin_New();
	duk_push_lstring(ctx, script->buffer, script->length);

	if (duk_peval(ctx) != 0) {
		JSPlugin_LogError(ctx, "loading script", script, NULL);
	}
	// TODO: Compile and call
	duk_destroy_heap(ctx);
}

static struct ChatCommand JSPlugin_Cmd = {
	"js", Scripting_Handle, false,
	{
		"&a/client js [script]",
		"&eExecutes the input text as a JavaScript script",
	}
};

static void Backend_Init(void) {
	Commands_Register(&JSPlugin_Cmd);
}
