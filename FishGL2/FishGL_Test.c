#include <cglm.h>

#define FGL_INLINE __forceinline
#include <FishGL.h>

FglBuffer ColorBuffer;
int Width;
int Height;

__declspec(dllexport) void fglDebugInit(int W, int H, int BPP) {
	Width = W;
	Height = H;

	void* ColorMemory = malloc(W * H * (BPP / 8));
	ColorBuffer = fglCreateBuffer(ColorMemory, W, H);

	fglClearBuffer(&ColorBuffer, (FglColor) { 50, 60, 80, 255 });
}

__declspec(dllexport) void fglDebugLoop() {
	FglColor White = { 255, 255, 255, 255 };

	fglDrawLine(&ColorBuffer, White, 100, 100, 300, 200);
	fglDrawLine(&ColorBuffer, White, 300, 200, 200, 500);
	fglDrawLine(&ColorBuffer, White, 200, 500, 100, 100);

	fglDisplayToFramebuffer(&ColorBuffer);
}