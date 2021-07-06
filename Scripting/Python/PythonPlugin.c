#define PY_SSIZE_T_CLEAN
#include <Python.h>
// https://bugs.python.org/issue27810
#define SCRIPTING_DIRECTORY "python"
#define SCRIPTING_ARGS PyObject* self, PyObject* args
#define SCRIPTING_CALL self, args
#define SCRIPTING_RESULT PyObject*
#define Scripting_DeclareFunc(name, func, num_args) { name, func, METH_VARARGS, "" }

#define Scripting_ReturnVoid() Py_RETURN_NONE // https://docs.python.org/3/c-api/none.html
#define Scripting_ReturnInt(value) return PyLong_FromLong(value)
#define Scripting_ReturnBool(value) return PyBool_FromLong(value)
#define Scripting_ReturnStr(buffer, len) return PyBytes_FromStringAndSize(buffer, len)
#define Scripting_ReturnPtr(value) return PyLong_FromVoidPtr(value)

#include "../../Scripting.h"


/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	cc_string str = emptyStr;
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return str;

	
	PyObject* obj = PyTuple_GET_ITEM(args, arg);
	str.buffer    = PyBytes_AsString(obj);
	str.length   = PyBytes_Size(obj);
	str.capacity = 0;
	return str;
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return 0;

	PyObject* obj = PyTuple_GET_ITEM(args, arg);
	return PyLong_AsLong(obj);
}

static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {
	sc_buffer buffer = { 0 };
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return buffer;

	PyObject* obj = PyTuple_GET_ITEM(args, arg);
	buffer.data = PyByteArray_AsString(obj);
	buffer.len  = PyByteArray_Size(obj);
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
	char buffer[NATIVE_STR_LEN+1];
	int i, len = min(NATIVE_STR_LEN, script->length);

	memcpy(buffer, script->buffer, len); buffer[len] = '\0';
	// newlines are pretty important in python
	for (i = 0; i < len; i++) {
		if (buffer[i] == '#') buffer[i] = '\n';
	}

	PyRun_SimpleString(buffer);
}

static struct ChatCommand PythonPlugin_Cmd = {
	"python", Scripting_Handle, false,
	{
		"&a/client python [script]",
		"&eExecutes the input text as a python script",
		"&e  Note: # is replaced with newlines",
	}
};

static void Backend_Init(void) {
	Commands_Register(&PythonPlugin_Cmd);
	PythonPlugin_Register();
	Py_SetProgramName(L"ClassiCube");
	Py_Initialize();
}
