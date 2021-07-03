// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API
#define CC_VAR

#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-config.h>

// references
//  https://stackoverflow.com/questions/46434387/could-not-load-assembly-system-when-using-c-and-embedded-mono-to-call-to-c-sha
//  https://www.mono-project.com/docs/advanced/embedding/

#include "src/String.h"
static MonoDomain* domain;

typedef union mono_arg {
	int i;
	MonoString* s;
} mono_arg;

typedef struct mono_func {
	const char* name;
	const void* func;
} mono_func;

static cc_string NETPlugin_GetString(mono_arg* ctx, int idx) {
	// TODO this leaks memory
	const char* chars = mono_string_to_utf8(ctx[idx].s);
	return String_FromReadonly(chars);
}

#define SCRIPTING_DIRECTORY "net"
#define SCRIPTING_CONTEXT mono_arg*
#define SCRIPTING_RESULT void*

#define Scripting_GetString(ctx, arg) NETPlugin_GetString(ctx, arg)
#define Scripting_GetInt(ctx, arg) ctx[arg].i
#define Scripting_Consume(ctx, args)

#define Scripting_ReturnVoid(ctx) return NULL;
#define Scripting_ReturnInt(ctx, value) return (void*)((cc_uintptr)value);
#define Scripting_ReturnBoolean(ctx, value) return (void*)((cc_uintptr)value);
#define Scripting_ReturnString(ctx, buffer, len) return mono_string_new_len(domain, buffer, len);

#include "Scripting.h"

/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
struct NETPlugin;
typedef struct NETPlugin { MonoAssembly* ctx; struct NETPlugin* next; } NETPlugin;
static NETPlugin* pluginsHead;

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
}


/*########################################################################################################################*
*--------------------------------------------------------Chat api---------------------------------------------------------*
*#########################################################################################################################*/
// TODO these are complete bogus
static const mono_func chatFuncs[] = {
	{ "Add",  CC_Chat_Add },
	{ "Send", CC_Chat_Send },
	{ NULL, NULL }
};


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Server_SendData(mono_arg* ctx) {
	return NULL;
	/*duk_size_t size;
	void* data = duk_to_buffer(ctx, -1, &size);
	Server.SendData(data, size);
	return 1;*/
}

static const mono_func serverFuncs[] = {
	{ "GetMotd",        CC_Server_GetMotd },
	{ "GetName",        CC_Server_GetName },
	{ "GetAppName",     CC_Server_GetAppName },
	{ "SetAppName",     CC_Server_SetAppName },
	{ "IsSingleplayer", CC_Server_IsSingleplayer },
	{ "SendData",       CC_Server_SendData},
	{ NULL, NULL }
};


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static const mono_func worldFuncs[] = {
	{ "GetWidth",  CC_World_GetWidth },
	{ "GetHeight", CC_World_GetHeight },
	{ "GetLength", CC_World_GetLength },
	{ "GetBlock",  CC_World_GetBlock },
	{ NULL, NULL }
};


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static const mono_func windowFuncs[] = {
	{ "SetTitle", CC_Window_SetTitle },
	{ NULL, NULL }
};


/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static void NETPlugin_RegisterModule(const char* name, const mono_func* funcs) {
	for (const mono_func* f = funcs; f->name; f++ ) {
	
		char buffer[256]; cc_string str;
		String_InitArray_NT(str, buffer);
		String_Format4(&str, "%c::%c", name, f->name, NULL, NULL);
		buffer[str.length] = '\0';
		
		puts(buffer);
		mono_add_internal_call(buffer, f->func);
	}
}

static void NETPlugin_Register(void) {
	NETPlugin_RegisterModule("Chat",   chatFuncs);
	NETPlugin_RegisterModule("Server", serverFuncs);
	NETPlugin_RegisterModule("World",  worldFuncs);
	NETPlugin_RegisterModule("Window", windowFuncs);
}

static void Backend_Load(const cc_string* path, void* obj) {
	static cc_string ext = String_FromConst(".dll");
	if (!String_CaselessEnds(path, &ext)) return;
	int res;
	
	char buffer[256]; cc_string str;
	String_InitArray_NT(str, buffer);
	String_Copy(&str, path);
	buffer[str.length] = '\0';

	MonoAssembly* assembly = mono_domain_assembly_open(domain, buffer);
	if (!assembly) {
		puts("failed to load"); return;
	}

	char* arg0 = "ClassiCube";
	mono_jit_exec(domain, assembly, 1, &arg0);

	// no need to bother freeing
	NETPlugin* plugin = Mem_Alloc(1, sizeof(NETPlugin), "net plugin");
	plugin->ctx  = assembly;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;
}

static void Backend_ExecScript(const cc_string* script) {
	Backend_Load(script, NULL);
}

static struct ChatCommand NETPlugin_Cmd = {
	"mono", Scripting_Handle, false,
	{
		"&a/client mono [pat",
		"&eExecutes and then loads the given .dll",
	}
};

static void Backend_Init(void) {
	Commands_Register(&NETPlugin_Cmd);
	mono_config_parse(NULL);
	domain = mono_jit_init("ClassiCube");
	NETPlugin_Register();
}
