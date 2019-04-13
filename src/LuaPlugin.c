// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)

#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/output/lua_plugin/include/lua.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/output/lua_plugin/include/lualib.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/output/lua_plugin/include/lauxlib.h"

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/GameStructs.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/Block.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/ExtMath.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/Game.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/Chat.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/Stream.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/TexturePack.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/World.h"
#include "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/Funcs.h"

#ifdef _WIN64
#pragma comment(lib, "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/x64/Debug/ClassiCube.lib")
#pragma comment(lib, "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/output/lua_plugin/liblua53.a")
#else
#pragma comment(lib, "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/src/x86/Debug/ClassiCube.lib")
#pragma comment(lib, "H:/PortableApps/GitPortable/App/Git/ClassicalSharp/output/lua_plugin/liblua53.a")
#endif

// ====== EXPORTED FUNCTIONS TO LUA =======
static int CC_AddChat(lua_State* L) {
	const char* msg = lua_tostring(L, -1);
	int len         = String_CalcLen(msg, UInt16_MaxValue);

	String str = String_Init(msg, len, len);
	Chat_Add(&str);
	return 0;
}

static struct {
	const char* Name;
	lua_CFunction Func;
} Lua_Funcs[] = {
	{ "addChat", CC_AddChat },
};

static void LuaPlugin_Init(void) {
	lua_State* L = luaL_newstate();
	luaL_openlibs(L);

	int i;
	for (i = 0; i < Array_Elems(Lua_Funcs); i++) {
		lua_register(L, Lua_Funcs[i].Name, Lua_Funcs[i].Func);
	}
	//luaL_dofile(L, "test.lua");
	int res1 = luaL_loadfile(L, "test.lua");
	int res2 = lua_pcall(L, 0, LUA_MULTRET, 0);
	const char* msg = lua_tostring(L, -1);
}

__declspec(dllexport) int Plugin_ApiVersion = 1;
__declspec(dllexport) struct IGameComponent Plugin_Component = {
	LuaPlugin_Init /* Init */
};
