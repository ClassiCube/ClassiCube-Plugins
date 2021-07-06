#define PY_SSIZE_T_CLEAN
#include <Python.h>

#define SCRIPTING_DIRECTORY "python"
#define SCRIPTING_ARGS PyObject* self, PyObject *const *args, Py_ssize_t numArgs
#define SCRIPTING_CALL self, args, numArgs
#define SCRIPTING_RESULT void*
#define Scripting_DeclareFunc(name, func, num_args) { name, func, METH_FASTCALL, "" }

#define Scripting_ReturnVoid() return NULL;
#define Scripting_ReturnInt(value) return PyLong_FromLong(value)
#define Scripting_ReturnBool(value) return PyBool_FromLong(value)
#define Scripting_ReturnStr(buffer, len) return PyBytes_FromStringAndSize(buffer, len)

#include "../../Scripting.h"


/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	return String_Empty;
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	return 0;
}

static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {
	sc_buffer buffer = { 0 };
	return buffer;
}

static void Scripting_FreeBuf(sc_buffer* buffer) {
	/* no need to manually free the memory here */
}

/*########################################################################################################################*
*--------------------------------------------------------Base API----------------------------------------------------------*
*#########################################################################################################################*/
struct PythonPlugin;
typedef struct PythonPlugin { void* ctx; struct PythonPlugin* next; } PythonPlugin;
static PythonPlugin* pluginsHead;

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
}

/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static PyMethodDef blockFuncs[] = { CC_BLOCK_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef blockModule  = { PyModuleDef_HEAD_INIT, "block", NULL, -1, blockFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_block(void) { return PyModule_Create(&blockModule); }

static PyMethodDef chatFuncs[] = { CC_CHAT_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef chatModule  = { PyModuleDef_HEAD_INIT, "chat", NULL, -1, chatFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_chat(void) { return PyModule_Create(&chatModule); }

static PyMethodDef serverFuncs[] = { CC_SERVER_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef serverModule  = { PyModuleDef_HEAD_INIT, "server", NULL, -1, serverFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_server(void) { return PyModule_Create(&serverModule); }

static PyMethodDef tablistFuncs[] = { CC_TABLIST_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef tablistModule  = { PyModuleDef_HEAD_INIT, "tablist", NULL, -1, tablistFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_tablist(void) { return PyModule_Create(&tablistModule); }

static PyMethodDef worldFuncs[] = { CC_WORLD_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef worldModule  = { PyModuleDef_HEAD_INIT, "world", NULL, -1, worldFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_world(void) { return PyModule_Create(&worldModule); }

static PyMethodDef windowFuncs[] = { CC_WINDOW_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef windowModule  = { PyModuleDef_HEAD_INIT, "window", NULL, -1, windowFuncs, NULL, NULL, NULL, NULL };
static PyObject* PyInit_window(void) { return PyModule_Create(&windowModule); }

static void PythonPlugin_Register(void) {
	PyImport_AppendInittab("block",   &PyInit_block);
	PyImport_AppendInittab("chat",    &PyInit_chat);
	PyImport_AppendInittab("server",  &PyInit_server);
	PyImport_AppendInittab("tablist", &PyInit_tablist);
	PyImport_AppendInittab("world",   &PyInit_world);
	PyImport_AppendInittab("window",  &PyInit_window);
}

static void Backend_Load(const cc_string* path, void* obj) {
/*	static cc_string ext = String_FromConst(".dll");
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
	PythonPlugin* plugin = Mem_Alloc(1, sizeof(PythonPlugin), "net plugin");
	plugin->ctx  = assembly;
	plugin->next = pluginsHead;
	pluginsHead  = plugin;*/
}

static void Backend_ExecScript(const cc_string* script) {
	char buffer[NATIVE_STR_LEN];
	Platform_EncodeUtf8(buffer, script);
	PyRun_SimpleString(buffer);
}

static struct ChatCommand PythonPlugin_Cmd = {
	"mono", Scripting_Handle, false,
	{
		"&a/client mono [pat",
		"&eExecutes and then loads the given .dll",
	}
};

static void Backend_Init(void) {
	Commands_Register(&PythonPlugin_Cmd);
	Py_SetProgramName(L"ClassiCube");
	Py_Initialize();
	PythonPlugin_Register();
}
