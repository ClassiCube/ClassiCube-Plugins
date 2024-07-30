#include "imgui.h"
#ifdef _WIN32
#define CC_API __declspec(dllimport)
#define CC_VAR __declspec(dllimport)
#pragma comment(lib, "C:/Tools/Git/repos/ClassiCube/src/x86/Debug/ClassiCube.lib")
#else
#define CC_API
#define CC_VAR
#endif

#include "../ClassiCube/src/Bitmap.h"
#include "../ClassiCube/src/Chat.h"
#include "../ClassiCube/src/Event.h"
#include "../ClassiCube/src/Game.h"
#include "../ClassiCube/src/Graphics.h"
#include "../ClassiCube/src/String.h"
#include "../ClassiCube/src/Platform.h"
#include "../ClassiCube/src/Window.h"
static GfxResourceID font_tex;

static void AllocFontTexture(void) {
	ImGuiIO& io = ImGui::GetIO();
    unsigned char* pixels;
    int width, height;
    io.Fonts->GetTexDataAsRGBA32(&pixels, &width, &height);

	struct Bitmap bmp;
	bmp.width  = width;
	bmp.height = height;
	bmp.scan0  = (BitmapCol*)pixels;
	font_tex   = Gfx_CreateTexture(&bmp, TEXTURE_FLAG_BILINEAR, false);

    io.Fonts->SetTexID((ImTextureID)(cc_uintptr)font_tex);
}

static void SetupState(ImDrawData* draw_data) {
	Gfx_SetViewport(0, 0, draw_data->DisplaySize.x, draw_data->DisplaySize.y);
	//Platform_Log2("VP: %f3,%f3", &draw_data->DisplaySize.x, &draw_data->DisplaySize.y);
}

static void FillIB(cc_uint16* indices, int count, void* obj) {
	memcpy(indices, obj, count * 2);
}

static void DrawCommand(const ImDrawCmd* pCmd, const ImDrawList* cmd_list, ImDrawData* draw_data) {
    ImVec2 clip_off = draw_data->DisplayPos;

	if (pCmd->UserCallback != nullptr)
	{
		// User callback, registered via ImDrawList::AddCallback()
		// (ImDrawCallback_ResetRenderState is a special callback value used by the user to request the renderer to reset render state.)
		if (pCmd->UserCallback == ImDrawCallback_ResetRenderState)
			SetupState(draw_data);
		else
			pCmd->UserCallback(cmd_list, pCmd);
	}
	else
	{
		// Project scissor/clipping rectangles into framebuffer space
		ImVec2 clip_min(pCmd->ClipRect.x - clip_off.x, pCmd->ClipRect.y - clip_off.y);
		ImVec2 clip_max(pCmd->ClipRect.z - clip_off.x, pCmd->ClipRect.w - clip_off.y);
		if (clip_max.x <= clip_min.x || clip_max.y <= clip_min.y)
			return;

		Gfx_SetScissor(clip_min.x, clip_min.y, clip_max.x - clip_min.x, clip_max.y - clip_min.y);
		//Platform_Log4("SC: %f3,%f3 %f3,%f3", &clip_min.x, &clip_min.y, &clip_max.x, &clip_max.y);
		Gfx_BindTexture((GfxResourceID)pCmd->GetTexID());

		// TODO not constantly recreate this..
		GfxResourceID ib = Gfx_CreateIb2(pCmd->ElemCount, FillIB, &cmd_list->IdxBuffer.Data[pCmd->IdxOffset]);
		Gfx_BindIb(ib);

		Gfx_DrawVb_IndexedTris_Range(pCmd->ElemCount * 4 / 6, pCmd->VtxOffset);

		Gfx_DeleteIb(&ib);
	}
}

static void Render2D(void) {
	ImDrawData* draw_data = ImGui::GetDrawData();
	SetupState(draw_data);
	Gfx_SetVertexFormat(VERTEX_FORMAT_TEXTURED);

    for (int n = 0; n < draw_data->CmdListsCount; n++)
    {
        const ImDrawList* cmd_list = draw_data->CmdLists[n];

		GfxResourceID vb = Gfx_CreateDynamicVb(VERTEX_FORMAT_TEXTURED, cmd_list->VtxBuffer.Size);
		void* tmp = Gfx_LockDynamicVb(vb, VERTEX_FORMAT_TEXTURED, cmd_list->VtxBuffer.Size);
		struct VertexTextured* dst = (struct VertexTextured*)tmp;

        for (int i = 0; i < cmd_list->VtxBuffer.Size; i++, dst++)
        {
            const ImDrawVert* src_v  = &cmd_list->VtxBuffer[i];
            
			dst->x   = src_v->pos.x;
			dst->y   = src_v->pos.y;
			dst->z   = 0.0f;
			dst->U   = src_v->uv.x;
			dst->V   = src_v->uv.y;
			dst->Col = src_v->col;
        }
		Gfx_UnlockDynamicVb(vb);

        for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
        {
            const ImDrawCmd* pcmd = &cmd_list->CmdBuffer[cmd_i];
			DrawCommand(pcmd, cmd_list, draw_data);
        }
		
		Gfx_DeleteVb(&vb);
    }

	Gfx_SetScissor( 0, 0, Window_Main.Width, Window_Main.Height);
	Gfx_SetViewport(0, 0, Window_Main.Width, Window_Main.Height);
}

static void Hook2D(float delta) {
    bool show_demo_window = true;
    bool show_another_window = false;
    ImVec4 clear_color = ImVec4(0.45f, 0.55f, 0.60f, 1.00f);

	if (!font_tex) {
		AllocFontTexture();
	}

	ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ImVec2(Window_Main.Width, Window_Main.Height);
    io.DeltaTime   = delta;

        ImGui::NewFrame();

        // 1. Show the big demo window (Most of the sample code is in ImGui::ShowDemoWindow()! You can browse its code to learn more about Dear ImGui!).
        ImGui::ShowDemoWindow(&show_demo_window);

        // 2. Show a simple window that we create ourselves. We use a Begin/End pair to create a named window.
        {
            static float f = 0.0f;
            static int counter = 0;

            ImGui::Begin("Hello, world!");                          // Create a window called "Hello, world!" and append into it.

            ImGui::Text("This is some useful text.");               // Display some text (you can use a format strings too)
            ImGui::Checkbox("Demo Window", &show_demo_window);      // Edit bools storing our window open/close state
            ImGui::Checkbox("Another Window", &show_another_window);

            ImGui::SliderFloat("float", &f, 0.0f, 1.0f);            // Edit 1 float using a slider from 0.0f to 1.0f
            ImGui::ColorEdit3("clear color", (float*)&clear_color); // Edit 3 floats representing a color

            if (ImGui::Button("Button"))                            // Buttons return true when clicked (most widgets return true when edited/activated)
                counter++;
            ImGui::SameLine();
            ImGui::Text("counter = %d", counter);

            ImGui::Text("Application average %.3f ms/frame (%.1f FPS)", 1000.0f / io.Framerate, io.Framerate);
            ImGui::End();
        }

        // Rendering
        ImGui::Render();
        Render2D();
}

static void OnContextLost(void* obj) {
	Gfx_DeleteTexture(&font_tex);
}

static void TestPlugin_Init(void) {
	IMGUI_CHECKVERSION();
	ImGui::CreateContext();

    Chat_Add1("%c", "imgui plugin loaded okay");

	ImGuiIO& io = ImGui::GetIO();
	io.ConfigFlags  |= ImGuiConfigFlags_NavEnableKeyboard;
	io.BackendFlags |= ImGuiBackendFlags_RendererHasVtxOffset;
	Game.Draw2DHooks[0] = Hook2D;

	Event_Register_(&GfxEvents.ContextLost, NULL, OnContextLost);
}

#ifdef CC_BUILD_WIN
    // special attribute to get symbols exported on Windows
    #define PLUGIN_EXPORT extern "C" __declspec(dllexport)
#else
    // public symbols already exported when compiling shared lib with GCC
    #define PLUGIN_EXPORT
#endif

PLUGIN_EXPORT int Plugin_ApiVersion = 1;
PLUGIN_EXPORT struct IGameComponent Plugin_Component = { TestPlugin_Init };
