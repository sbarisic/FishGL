#pragma once
#include <stdint.h>
#include <string.h>
#include <stdbool.h>

#ifndef FGL_API
#define FGL_API
#endif

#ifndef FGL_INLINE
#define FGL_INLINE
#endif

typedef enum {
	PixelOrder_Unknown,
	PixelOrder_RGBA,
	PixelOrder_ABGR
} PixelOrder;

typedef struct {
	void* VideoMemory;
	int32_t Width;
	int32_t Height;
	int32_t BPP;
	int32_t Stride;
	PixelOrder Order;
} FglState;

typedef struct {
	uint8_t R, G, B, A;
} FglColor;

typedef struct {
	union {
		void* Memory;
		FglColor* Pixels;
	};

	int32_t Width;
	int32_t Height;

	int32_t Length;
	int32_t PixelCount;
} FglBuffer;

// Initialization
FGL_API FGL_INLINE void fglInit(void* VideoMemory, int32_t Width, int32_t Height, int32_t BPP, int32_t Stride, PixelOrder Order);

// Buffer functions
FGL_API FGL_INLINE FglBuffer fglCreateBuffer(void* Memory, int32_t Width, int32_t Height);
FGL_API FGL_INLINE void fglDisplayToFramebuffer(FglBuffer* Buffer);
FGL_API FGL_INLINE void fglClearBuffer(FglBuffer* Buffer, FglColor Clr);

// Drawing
FGL_API FGL_INLINE void fglDrawLine(FglBuffer* Buffer, FglColor Color, int32_t X0, int32_t Y0, int32_t X1, int32_t Y1);