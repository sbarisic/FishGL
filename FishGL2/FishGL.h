#pragma once
#include <stdint.h>

#ifndef FGL_API
#define FGL_API
#endif

typedef enum {
	PixelOrder_Unknown,
	PixelOrder_RGBA,
	PixelOrder_ABGR
} PixelOrder;

typedef struct {
	void* VideoMemory;
} FglState;

FGL_API void fglInit(void* VideoMemory, int32_t Width, int32_t Height, int32_t BPP, int32_t Stride, PixelOrder Order);

// Test

FGL_API void fglDebugLoop();