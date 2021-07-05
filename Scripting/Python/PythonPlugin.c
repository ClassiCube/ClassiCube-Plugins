// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API
#define CC_VAR

#define PY_SSIZE_T_CLEAN
#include <Python.h>

#include "src/String.h"
static cc_string PythonPlugin_GetStr(Py_ssize_t ctx, int idx) {
	return String_Empty;
}

static int PythonPlugin_GetInt(Py_ssize_t ctx, int idx) {
	return 0;
}

#define SCRIPTING_DIRECTORY "python"
#define SCRIPTING_CONTEXT PyObject* self, PyObject *const *args, Py_ssize_t
#define SCRIPTING_RESULT void*
#define Scripting_DeclareFunc(name, func, num_args) { name, func, METH_FASTCALL, "" }
#define Scripting_GetStr(ctx, arg) PythonPlugin_GetStr(ctx, arg)
#define Scripting_GetInt(ctx, arg) PythonPlugin_GetInt(ctx, arg)
#define Scripting_Consume(ctx, args)

#define Scripting_ReturnVoid(ctx) return NULL;
#define Scripting_ReturnInt(ctx, value) return PyLong_FromLong(value)
#define Scripting_ReturnBoolean(ctx, value) return PyBool_FromLong(value)
#define Scripting_ReturnString(ctx, buffer, len) return PyBytes_FromStringAndSize(buffer, len)

#include "Scripting.h"

/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
struct PythonPlugin;
typedef struct PythonPlugin { void* ctx; struct PythonPlugin* next; } PythonPlugin;
static PythonPlugin* pluginsHead;

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
}

static PyMethodDef chatFuncs[] = { CC_CHAT_FUNCS, SCRIPTING_NULL_FUNC };
static PyModuleDef chatModule = {
	PyModuleDef_HEAD_INIT, "chat", NULL, -1, chatFuncs,
	NULL, NULL, NULL, NULL
};

static PyObject* PyInit_chat(void) { return PyModule_Create(&chatModule); }

static void PythonPlugin_Register(void) {
	PyImport_AppendInittab("chat", &PyInit_chat);
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
	Platform_EncodeUtf8(script, buffer);
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
