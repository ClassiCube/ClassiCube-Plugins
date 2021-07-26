#define PY_SSIZE_T_CLEAN
#include <Python.h>
// https://bugs.python.org/issue27810
#define SCRIPTING_DIRECTORY "python"
#define SCRIPTING_ARGS PyObject* self, PyObject* args
#define SCRIPTING_CALL self, args
#define SCRIPTING_RESULT PyObject*
#define SCRIPTING_FUNC PyMethodDef
#define Scripting_DeclareFunc(name, func, num_args) { name, func, METH_VARARGS, "" }

#define Scripting_ReturnVoid() Py_RETURN_NONE // https://docs.python.org/3/c-api/none.html
#define Scripting_ReturnInt(value) return PyLong_FromLong(value)
#define Scripting_ReturnBool(value) return PyBool_FromLong(value)
#define Scripting_ReturnStr(buffer, len) return PyBytes_FromStringAndSize(buffer, len)
#define Scripting_ReturnPtr(value) return PyLong_FromVoidPtr(value)
#define Scripting_ReturnNum(value) return PyFloat_FromDouble(value)

#include "../../Scripting.h"


/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static sc_buffer emptyBuf;
static cc_string MakeString(const char* buffer, int len) {
	cc_string str;
	str.buffer    = buffer;
	str.length   = len;
	str.capacity = 0;
	return str;
}

static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return emptyStr;
	PyObject* obj = PyTuple_GET_ITEM(args, arg);

	if (PyBytes_Check(obj)) {
		const char* buf = PyBytes_AsString(obj);
		int len         = PyBytes_Size(obj);
		return MakeString(buf, len);
	} else {
		PyObject* repr = PyObject_Repr(obj);
		Py_XDECREF(repr);
		return emptyStr;
	}
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return 0;
	PyObject* obj = PyTuple_GET_ITEM(args, arg);

	return PyLong_AsLong(obj);
}

static double Scripting_GetNum(SCRIPTING_ARGS, int arg) {
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return 0;
	PyObject* obj = PyTuple_GET_ITEM(args, arg);

	return PyFloat_AsDouble(obj);
}

static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {	
	Py_ssize_t nargs = PyTuple_GET_SIZE(args);
	if (arg >= nargs) return emptyBuf;
	PyObject* obj = PyTuple_GET_ITEM(args, arg);

	sc_buffer buffer;
	buffer.data = PyByteArray_AsString(obj);
	buffer.len  = PyByteArray_Size(obj);
	return buffer;
}

static void Scripting_FreeStr(cc_string* str) {
	// no need to manually free the memory here
}

static void Scripting_FreeBuf(sc_buffer* buf) {
	// no need to manually free the memory here
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
#define STR(x) #x
#define PluginModule(modname) \
	static PyModuleDef modname ## Module = \
	{ PyModuleDef_HEAD_INIT, STR(modname), NULL, -1, modname ## Funcs, NULL, NULL, NULL, NULL }; \
	static PyObject* PyInit_ ## modname(void) { return PyModule_Create(&modname ## Module); }

PluginModule(block)
PluginModule(camera)
PluginModule(chat)
PluginModule(env)
PluginModule(game)
PluginModule(inventory)
PluginModule(player)
PluginModule(server)
PluginModule(tablist)
PluginModule(world)
PluginModule(window)

static void PythonPlugin_Register(void) {
	PyImport_AppendInittab("block",     &PyInit_block);
	PyImport_AppendInittab("camera",    &PyInit_camera);
	PyImport_AppendInittab("chat",      &PyInit_chat);
	PyImport_AppendInittab("env",       &PyInit_env);
	PyImport_AppendInittab("game",      &PyInit_game);
	PyImport_AppendInittab("inventory", &PyInit_inventory);
	PyImport_AppendInittab("player",    &PyInit_player);
	PyImport_AppendInittab("server",    &PyInit_server);
	PyImport_AppendInittab("tablist",   &PyInit_tablist);
	PyImport_AppendInittab("world",     &PyInit_world);
	PyImport_AppendInittab("window",    &PyInit_window);
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
