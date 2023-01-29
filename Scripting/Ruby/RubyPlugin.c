#include <ruby.h>
// https://silverhammermba.github.io/emberb/c/
// https://silverhammermba.github.io/emberb/embed/
// https://silverhammermba.github.io/emberb/examples/
// https://tristanpenman.com/blog/posts/2018/09/16/extending-ruby-with-cpp/
// https://stackoverflow.com/questions/19249467/c-in-ruby-c-extensions-pointer-problems
// https://ruby-hacking-guide.github.io/class.html
// https://www.rubyguides.com/2018/03/write-ruby-c-extension/
// https://stackoverflow.com/questions/228648/how-do-you-list-the-currently-available-objects-in-the-current-scope-in-ruby
// https://sonots.github.io/ruby-capi/d9/d2d/sprintf_8c_source.html#l01452
// https://stackoverflow.com/questions/25488902/what-happens-when-you-use-string-interpolation-in-ruby
// https://github.com/ruby/ruby/blob/96db72ce38b27799dd8e80ca00696e41234db6ba/doc/extension.rdoc#encapsulate-c-data-into-a-ruby-object

typedef struct ruby_function {
	const char* name;
	void* value;
} ruby_function;

#define SCRIPTING_DIRECTORY "ruby"
#define SCRIPTING_ARGS int argc, VALUE* argv, VALUE self
#define SCRIPTING_CALL argc, argv, self
#define SCRIPTING_RESULT VALUE
#define SCRIPTING_FUNC ruby_function
#define Scripting_DeclareFunc(name, func, num_args) { name, func }

#define Scripting_ReturnVoid() return Qnil
#define Scripting_ReturnInt(value) return rb_int_new(value)
#define Scripting_ReturnBool(value) return value ? Qtrue : Qfalse
#define Scripting_ReturnStr(buffer, len) return rb_str_new(buffer, len)
#define Scripting_ReturnPtr(value) return rb_ll2inum((LONG_LONG)value)
#define Scripting_ReturnNum(value) return rb_float_new(value)

#include "../../Scripting.h"


/*########################################################################################################################*
*--------------------------------------------------------Backend----------------------------------------------------------*
*#########################################################################################################################*/
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg) {
	if (arg >= argc) return emptyStr;
	VALUE obj = argv[arg];

	obj = rb_obj_as_string(obj); // TODO prob mem leak here
	cc_string str;
	str.buffer   = RSTRING_PTR(obj);
	str.length   = RSTRING_LEN(obj);
	str.capacity = 0;
	return str;
}

static int Scripting_GetInt(SCRIPTING_ARGS, int arg) {
	if (arg >= argc) return 0;
	VALUE obj = argv[arg];

	return NUM2INT(obj);
}

static double Scripting_GetNum(SCRIPTING_ARGS, int arg) {
	if (arg >= argc) return 0;
	VALUE obj = argv[arg];

	return RFLOAT_VALUE(obj);
}

static sc_buffer emptyBuf;
static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg) {	
	if (arg >= argc) return emptyBuf;
	VALUE obj = argv[arg];

	sc_buffer buffer;
	buffer.data = RSTRING_PTR(obj);
	buffer.len  = RSTRING_LEN(obj);
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
struct RubyPlugin;
typedef struct RubyPlugin { void* ctx; struct RubyPlugin* next; } RubyPlugin;
static RubyPlugin* pluginsHead;

static cc_string Backend_GetError(SCRIPTING_ARGS) {
	VALUE exception = rb_errinfo();
	rb_set_errinfo(Qnil);
		
	if (!RTEST(exception)) return emptyStr;
	VALUE err = rb_funcall(exception, rb_intern("message"), 0);
	//const char* klass = rb_obj_classname(err);

	cc_string str;
	str.buffer   = RSTRING_PTR(err);
	str.length   = RSTRING_LEN(err);
	str.capacity = 0;
	return str;
}

static void Backend_RaiseVoid(const char* groupName, const char* funcName) {
}

static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType) {
}

/*########################################################################################################################*
*-------------------------------------------------Plugin implementation---------------------------------------------------*
*#########################################################################################################################*/
static void RubyPlugin_RegisterModule(const char* name, const ruby_function* funcs) {
	VALUE mod = rb_define_module(name);
	const ruby_function* func;
	
	for (func = funcs; func->name; func++) {
		rb_define_module_function(mod, func->name, func->value, -1);
	}
}

static void RubyPlugin_Register(void) {
	// lowercase module names don't work
	RubyPlugin_RegisterModule("Block",     blockFuncs);
	RubyPlugin_RegisterModule("Camera",    cameraFuncs);
	RubyPlugin_RegisterModule("Env",       envFuncs);
	RubyPlugin_RegisterModule("Chat",      chatFuncs);
	RubyPlugin_RegisterModule("Game",      gameFuncs);
	RubyPlugin_RegisterModule("Inventory", inventoryFuncs);
	RubyPlugin_RegisterModule("Player",    playerFuncs);
	RubyPlugin_RegisterModule("Server",    serverFuncs);
	RubyPlugin_RegisterModule("Tablist",   tablistFuncs);
	RubyPlugin_RegisterModule("World",     worldFuncs);
	RubyPlugin_RegisterModule("Window",    windowFuncs);
}

static void RunScript(const char* script) {
	int state;
	VALUE result = rb_eval_string_protect(script, &state);
	if (!state) return;
	
	printf("ERROR: %i\n", state);
	Scripting_LogError(0, NULL, 0,
			"executing script", NULL, NULL);
}

static void Backend_Load(const cc_string* path, void* obj) {
}

static void Backend_ExecScript(const cc_string* script) {
	char buffer[NATIVE_STR_LEN+1];
	int i, len = min(NATIVE_STR_LEN, script->length);
	memcpy(buffer, script->buffer, len); buffer[len] = '\0';

	// newlines are pretty important in ruby
	for (i = 0; i < len; i++) {
		if (buffer[i] == '#') buffer[i] = '\n';
	}
	RunScript(buffer);
}

static struct ChatCommand PythonPlugin_Cmd = {
	"ruby", Scripting_Handle, false,
	{
		"&a/client ruby [script]",
		"&eExecutes the input text as a ruby script",
		"&e  Note: # is replaced with newlines",
	}
};

static void Backend_Init(void) {
	Commands_Register(&PythonPlugin_Cmd);
	ruby_init();
	RubyPlugin_Register();
}