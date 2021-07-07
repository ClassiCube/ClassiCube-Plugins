// NOTE: Make sure the Scripting.h files in the various backend folders
//  are kept up to date with the Scripting.h file in the root folder
// ====================================================================

// Since we are building an external plugin dll, we need to import from ClassiCube lib instead of exporting these
#ifdef _WIN32
  #define CC_API __declspec(dllimport)
  #define CC_VAR __declspec(dllimport)
  #ifdef _WIN64
    #pragma comment(lib, "../../../../ClassicalSharp/src/x64/Debug/ClassiCube.lib")
  #else
    #pragma comment(lib, "../../../../ClassicalSharp/src/x86/Debug/ClassiCube.lib")
  #endif
#else
  #define CC_API
  #define CC_VAR
#endif

// The proper way would be to add 'additional include directories' and 'additional libs' in Visual Studio Project properties
// Or, you can just be lazy and change these paths for your own system. 
// You must compile ClassiCube in both x86 and x64 configurations to generate the .lib file.
#include "../../ClassicalSharp/src/Game.h"
#include "../../ClassicalSharp/src/String.h"
#include "../../ClassicalSharp/src/Block.h"
#include "../../ClassicalSharp/src/Entity.h"
#include "../../ClassicalSharp/src/ExtMath.h"
#include "../../ClassicalSharp/src/Chat.h"
#include "../../ClassicalSharp/src/Stream.h"
#include "../../ClassicalSharp/src/TexturePack.h"
#include "../../ClassicalSharp/src/World.h"
#include "../../ClassicalSharp/src/Funcs.h"
#include "../../ClassicalSharp/src/Event.h"
#include "../../ClassicalSharp/src/Server.h"
#include "../../ClassicalSharp/src/Window.h"
#include "../../ClassicalSharp/src/Camera.h"
#include "../../ClassicalSharp/src/Inventory.h"

static void Backend_RaiseVoid(const char* groupName, const char* funcName);
static void Backend_RaiseChat(const char* groupName, const char* funcName, const cc_string* msg, int msgType);

static void Backend_Load(const cc_string* origName, void* obj);
static void Backend_ExecScript(const cc_string* script);
static void Backend_Init(void);

/*
Backends must provide the following defines:
#define SCRIPTING_DIRECTORY
#define SCRIPTING_ARGS
#define SCRIPTING_CALL
#define SCRIPTING_RESULT
---------------------------
NOTE that SCRIPTING_ARGS is implicitly available to all of the following macros:

#define Scripting_ReturnVoid()
#define Scripting_ReturnInt(value)
#define Scripting_ReturnBool(value)
#define Scripting_ReturnStr(buffer, len)
#define Scripting_ReturnPtr(value)
#define Scripting_ReturnNum(value)
*/

#define SCRIPTING_NULL_FUNC Scripting_DeclareFunc(NULL, NULL, 0)
static const cc_string emptyStr = { "", 0, 0 };
struct sc_buffer_ { cc_uint8* data; int len, meta; };

typedef struct sc_buffer_ sc_buffer;
typedef struct cc_string_ cc_string;
static cc_string Scripting_GetStr(SCRIPTING_ARGS, int arg);
static sc_buffer Scripting_GetBuf(SCRIPTING_ARGS, int arg);
static int       Scripting_GetInt(SCRIPTING_ARGS, int arg);
static void      Scripting_FreeBuf(sc_buffer* buffer);


/*########################################################################################################################*
*--------------------------------------------------------Block api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Block_Parse(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	int block     = Block_Parse(&str);
	Scripting_ReturnInt(block);
}

static SCRIPTING_FUNC blockFuncs[] = {     
	Scripting_DeclareFunc("parse", CC_Block_Parse, 1),
	SCRIPTING_NULL_FUNC 
};


/*########################################################################################################################*
*--------------------------------------------------------Chat api---------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Chat_Add(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	Chat_Add(&str);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Chat_AddOf(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	int msgType   = Scripting_GetInt(SCRIPTING_CALL, 1);
	Chat_AddOf(&str, msgType);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Chat_Send(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	Chat_Send(&str, false);
	Scripting_ReturnVoid();
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

static SCRIPTING_FUNC chatFuncs[] = {
	Scripting_DeclareFunc("add",   CC_Chat_Add,   1),
	Scripting_DeclareFunc("addOf", CC_Chat_AddOf, 2),
	Scripting_DeclareFunc("send",  CC_Chat_Send,  1),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*-------------------------------------------------------Camera api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Camera_GetFOV(SCRIPTING_ARGS) {
	Scripting_ReturnInt(Camera.Fov);
}

static SCRIPTING_RESULT CC_Camera_IsThird(SCRIPTING_ARGS) {
	Scripting_ReturnBool(Camera.Active->isThirdPerson);
}

static SCRIPTING_RESULT CC_Camera_GetX(SCRIPTING_ARGS) {
	Scripting_ReturnNum(Camera.CurrentPos.X);
}
static SCRIPTING_RESULT CC_Camera_GetY(SCRIPTING_ARGS) {
	Scripting_ReturnNum(Camera.CurrentPos.Y);
}
static SCRIPTING_RESULT CC_Camera_GetZ(SCRIPTING_ARGS) {
	Scripting_ReturnNum(Camera.CurrentPos.Z);
}

static SCRIPTING_RESULT CC_Camera_GetYaw(SCRIPTING_ARGS) {
	Vec2 orientation = Camera.Active->GetOrientation();
	Scripting_ReturnNum(orientation.X);
}
static SCRIPTING_RESULT CC_Camera_GetPitch(SCRIPTING_ARGS) {
	Vec2 orientation = Camera.Active->GetOrientation();
	Scripting_ReturnNum(orientation.Y);
}

static SCRIPTING_FUNC cameraFuncs[] = {
	Scripting_DeclareFunc("getFOV",   CC_Camera_GetFOV,   0),
	Scripting_DeclareFunc("isThird",  CC_Camera_IsThird,  0),
	Scripting_DeclareFunc("getX",     CC_Camera_GetX,     0),
	Scripting_DeclareFunc("getY",     CC_Camera_GetY,     0),
	Scripting_DeclareFunc("getZ",     CC_Camera_GetZ,     0),
	Scripting_DeclareFunc("getYaw",   CC_Camera_GetYaw,   0),
	Scripting_DeclareFunc("getPitch", CC_Camera_GetPitch, 0),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*-----------------------------------------------------Inventory api-------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Inventory_GetSelected(SCRIPTING_ARGS) {
	Scripting_ReturnInt(Inventory_SelectedBlock);
}

static SCRIPTING_FUNC inventoryFuncs[] = {
	Scripting_DeclareFunc("getSelected", CC_Inventory_GetSelected, 0),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*-------------------------------------------------------Player api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Player_GetReach(SCRIPTING_ARGS) {
	struct LocalPlayer* p = (struct LocalPlayer*)Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(p->ReachDistance);
}

static SCRIPTING_RESULT CC_Player_GetX(SCRIPTING_ARGS) {
	struct Entity* e = Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(e->Position.X);
}
static SCRIPTING_RESULT CC_Player_GetY(SCRIPTING_ARGS) {
	struct Entity* e = Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(e->Position.Y);
}
static SCRIPTING_RESULT CC_Player_GetZ(SCRIPTING_ARGS) {
	struct Entity* e = Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(e->Position.Z);
}

static SCRIPTING_RESULT CC_Player_GetYaw(SCRIPTING_ARGS) {
	struct Entity* e = Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(e->Yaw);
}
static SCRIPTING_RESULT CC_Player_GetPitch(SCRIPTING_ARGS) {
	struct Entity* e = Entities.List[ENTITIES_SELF_ID];
	Scripting_ReturnNum(e->Pitch);
}

static SCRIPTING_FUNC playerFuncs[] = {
	Scripting_DeclareFunc("getReach", CC_Player_GetReach, 0),
	Scripting_DeclareFunc("getX",     CC_Player_GetX,     0),
	Scripting_DeclareFunc("getY",     CC_Player_GetY,     0),
	Scripting_DeclareFunc("getZ",     CC_Player_GetZ,     0),
	Scripting_DeclareFunc("getYaw",   CC_Player_GetYaw,   0),
	Scripting_DeclareFunc("getPitch", CC_Player_GetPitch, 0),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*-------------------------------------------------------Server api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Server_GetMotd(SCRIPTING_ARGS) {
	Scripting_ReturnStr(Server.MOTD.buffer, Server.MOTD.length);
}
static SCRIPTING_RESULT CC_Server_GetName(SCRIPTING_ARGS) {
	Scripting_ReturnStr(Server.Name.buffer, Server.Name.length);
}
static SCRIPTING_RESULT CC_Server_GetAppName(SCRIPTING_ARGS) {
	Scripting_ReturnStr(Server.AppName.buffer, Server.AppName.length);
}

static SCRIPTING_RESULT CC_Server_SetAppName(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	String_Copy(&Server.AppName, &str);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Server_SendData(SCRIPTING_ARGS) {
	sc_buffer buffer = Scripting_GetBuf(SCRIPTING_CALL, 0);
	Server.SendData(buffer.data, buffer.len);

	Scripting_FreeBuf(&buffer);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Server_IsSingleplayer(SCRIPTING_ARGS) {
	Scripting_ReturnBool(Server.IsSinglePlayer);
}

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

static SCRIPTING_FUNC serverFuncs[] = {
	Scripting_DeclareFunc("getMotd",     CC_Server_GetMotd,    0),
	Scripting_DeclareFunc("getName",     CC_Server_GetName,    0),
	Scripting_DeclareFunc("getAppName",  CC_Server_GetAppName, 0),
	Scripting_DeclareFunc("setAppName",  CC_Server_SetAppName, 1),
	Scripting_DeclareFunc("sendData",    CC_Server_SendData,   1),
	Scripting_DeclareFunc("isSingleplayer", CC_Server_IsSingleplayer, 0),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*-------------------------------------------------------Tablist api-------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Tablist_GetPlayer(SCRIPTING_ARGS) {
	int id         = Scripting_GetInt(SCRIPTING_CALL, 0);
	cc_string name = emptyStr;
	if (TabList.NameOffsets[id]) name = TabList_UNSAFE_GetPlayer(id);

	Scripting_ReturnStr(name.buffer, name.length);
}

static SCRIPTING_RESULT CC_Tablist_GetName(SCRIPTING_ARGS) {
	int id         = Scripting_GetInt(SCRIPTING_CALL, 0);
	cc_string name = emptyStr;
	if (TabList.NameOffsets[id]) name = TabList_UNSAFE_GetList(id);

	Scripting_ReturnStr(name.buffer, name.length);
}

static SCRIPTING_RESULT CC_Tablist_GetGroup(SCRIPTING_ARGS) {
	int id         = Scripting_GetInt(SCRIPTING_CALL, 0);
	cc_string name = emptyStr;
	if (TabList.NameOffsets[id]) name = TabList_UNSAFE_GetGroup(id);

	Scripting_ReturnStr(name.buffer, name.length);
}

static SCRIPTING_RESULT CC_Tablist_GetRank(SCRIPTING_ARGS) {
	int id = Scripting_GetInt(SCRIPTING_CALL, 0);
	Scripting_ReturnInt(TabList.GroupRanks[id]);
}

static SCRIPTING_RESULT CC_Tablist_Remove(SCRIPTING_ARGS) {
	int id = Scripting_GetInt(SCRIPTING_CALL, 0);
	TabList_Remove(id);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Tablist_Set(SCRIPTING_ARGS) {
	int id           = Scripting_GetInt(SCRIPTING_CALL, 0);
	cc_string player = Scripting_GetStr(SCRIPTING_CALL, 1);
	cc_string list   = Scripting_GetStr(SCRIPTING_CALL, 2);
	cc_string group  = Scripting_GetStr(SCRIPTING_CALL, 3);
	int groupRank    = Scripting_GetInt(SCRIPTING_CALL, 4);
	TabList_Set(id, &player, &list, &group, groupRank);

	Scripting_ReturnVoid();
}

static SCRIPTING_FUNC tablistFuncs[] = {
	Scripting_DeclareFunc("getPlayer", CC_Tablist_GetPlayer, 1),
	Scripting_DeclareFunc("getName",   CC_Tablist_GetName,   1),
	Scripting_DeclareFunc("getGroup",  CC_Tablist_GetGroup,  1),
	Scripting_DeclareFunc("getRank",   CC_Tablist_GetRank,   1),
	Scripting_DeclareFunc("remove",    CC_Tablist_Remove,    1),
	Scripting_DeclareFunc("set",       CC_Tablist_Set,       5),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*--------------------------------------------------------World api--------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_World_GetWidth(SCRIPTING_ARGS) {
	Scripting_ReturnInt(World.Width);
}

static SCRIPTING_RESULT CC_World_GetHeight(SCRIPTING_ARGS) {
	Scripting_ReturnInt(World.Height);
}

static SCRIPTING_RESULT CC_World_GetLength(SCRIPTING_ARGS) {
	Scripting_ReturnInt(World.Length);
}

static SCRIPTING_RESULT CC_World_GetBlock(SCRIPTING_ARGS) {
	int x = Scripting_GetInt(SCRIPTING_CALL, 0);
	int y = Scripting_GetInt(SCRIPTING_CALL, 1);
	int z = Scripting_GetInt(SCRIPTING_CALL, 2);
	Scripting_ReturnInt(World_GetBlock(x, y, z));
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

static SCRIPTING_FUNC worldFuncs[] = {
	Scripting_DeclareFunc("getWidth",  CC_World_GetWidth,  0),
	Scripting_DeclareFunc("getHeight", CC_World_GetHeight, 0),
	Scripting_DeclareFunc("getLength", CC_World_GetLength, 0),
	Scripting_DeclareFunc("getBlock",  CC_World_GetBlock,  3),
	SCRIPTING_NULL_FUNC
};


/*########################################################################################################################*
*--------------------------------------------------------Window api-------------------------------------------------------*
*#########################################################################################################################*/
static SCRIPTING_RESULT CC_Window_SetTitle(SCRIPTING_ARGS) {
	cc_string str = Scripting_GetStr(SCRIPTING_CALL, 0);
	Window_SetTitle(&str);
	Scripting_ReturnVoid();
}

static SCRIPTING_RESULT CC_Window_GetHandle(SCRIPTING_ARGS) {
	Scripting_ReturnPtr(WindowInfo.Handle);
}

static SCRIPTING_FUNC windowFuncs[] = {
	Scripting_DeclareFunc("setTitle",  CC_Window_SetTitle,  1),
	Scripting_DeclareFunc("getHandle", CC_Window_GetHandle, 0), 
	SCRIPTING_NULL_FUNC
};


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
