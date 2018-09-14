#include <cglm.h>

#define FGL_INLINE __forceinline
#define FGL_API __declspec(dllexport)
#include <FishGL.h>

FglState RenderState;

FGL_INLINE void fglInit(void* VideoMemory, int32_t Width, int32_t Height, int32_t BPP, int32_t Stride, PixelOrder Order) {
	RenderState.VideoMemory = VideoMemory;
	RenderState.Width = Width;
	RenderState.Height = Height;
	RenderState.BPP = BPP;
	RenderState.Stride = Stride;
	RenderState.Order = Order;
}

// Buffers

FGL_INLINE FglBuffer fglCreateBuffer(void* Memory, int32_t Width, int32_t Height) {
	FglBuffer Buffer;
	Buffer.Memory = Memory;
	Buffer.Length = Width * Height * sizeof(FglColor);
	Buffer.PixelCount = Width * Height;
	Buffer.Width = Width;
	Buffer.Height = Height;
	return Buffer;
}

FGL_INLINE void fglClearBuffer(FglBuffer* Buffer, FglColor Clr) {
	for (size_t i = 0; i < Buffer->PixelCount; i++)
		Buffer->Pixels[i] = Clr;
}

FGL_INLINE void fglDisplayToFramebuffer(FglBuffer* Buffer) {
	if (RenderState.BPP == 32 && RenderState.Stride == 0 && RenderState.Order == PixelOrder_RGBA) {
		memcpy(RenderState.VideoMemory, Buffer->Memory, Buffer->Length);
		return;
	}

	// TODO: Handle stride, pixel order and pixel size
}

// Drawing

FGL_INLINE void fglDrawLine(FglBuffer* Buffer, FglColor Color, int32_t X0, int32_t Y0, int32_t X1, int32_t Y1) {
	bool Steep = false;

	if (abs(X0 - X1) < abs(Y0 - Y1)) {
		int Tmp = X0;
		X0 = Y0;
		Y0 = Tmp;

		Tmp = X1;
		X1 = Y1;
		Y1 = Tmp;

		Steep = true;
	}

	if (X0 > X1) {
		int Tmp = X0;
		X0 = X1;
		X1 = Tmp;

		Tmp = Y0;
		Y0 = Y1;
		Y1 = Tmp;
	}

	int DeltaX = X1 - X0;
	int DeltaY = Y1 - Y0;
	int DeltaError2 = abs(DeltaY) * 2;
	int Error2 = 0;
	int Y = Y0;

	for (int X = X0; X <= X1; X++) {
		if (Steep) {
			if (X < 0 || X >= Buffer->Height || Y < 0 || Y >= Buffer->Width)
				continue;

			Buffer->Pixels[X * Buffer->Width + Y] = Color;
		}
		else {
			if (Y < 0 || Y >= Buffer->Height || X < 0 || X >= Buffer->Width)
				continue;

			Buffer->Pixels[Y * Buffer->Width + X] = Color;
		}

		Error2 += DeltaError2;

		if (Error2 > DeltaX) {
			Y += (Y1 > Y0 ? 1 : -1);
			Error2 -= DeltaX * 2;
		}
	}
}