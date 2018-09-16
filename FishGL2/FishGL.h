#pragma once
#include <FishGLConfig.h>

#define XElement 0
#define YElement 1
#define ZElement 2
#define WElement 3

#define FGL_DISCARD true
#define FGL_KEEP false

#define COPY_VEC2(dst, src) do { dst[XElement] = src[XElement]; dst[YElement] = src[YElement]; } while(0)

typedef enum {
	PixelOrder_Unknown,
	PixelOrder_RGBA,
	PixelOrder_ABGR
} PixelOrder;

/*
typedef struct {
	union {
		vec2 V2;
		vec3 V3;
	};
} vec23;*/

typedef struct {
	union {
		struct {
			uint8_t R, G, B, A;
		};
		int32_t Int;
		uint32_t UInt;
	};
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

typedef struct {
	vec3 A, B, C;
} FglTriangle3;

typedef struct {
	vec2 A, B, C;
} FglTriangle2;

typedef enum {
	FglTextureWrap_Clamp,
	FglTextureWrap_BorderColor,
	//FglTextureWrap_Repeat,
} FglTextureWrap;

typedef enum {
	FglBlendMode_None
} FglBlendMode;

typedef enum {
	FglShaderType_Vertex,
	FglShaderType_Fragment,
} FglShaderType;

// These are not vec3 and vec2 because of padding
typedef union {
	float Vec3[3];
	float Vec2[2];
} FglVarying;

typedef union {
	mat3 Mat;

	struct {
		FglVarying A;
		FglVarying B;
		FglVarying C;
	};
} FglVaryingIn;

typedef struct {
	void* VideoMemory;
	int32_t Width;
	int32_t Height;
	int32_t BPP;
	int32_t Stride;
	PixelOrder Order;

	FglBlendMode BlendMode;
	FglTextureWrap TextureWrap;

	FglColor BorderColor;
	FglBuffer Textures[FGL_MAX_TEXTURES];

	FglVaryingIn VarIn[FGL_VARYING_COUNT];
	FglVarying VarOut[FGL_VARYING_COUNT];

	int32_t VertNum;
	FglShaderType CurShader;

	void* VertexShader;
	void* FragmentShader;
} FglState;

#ifdef FGL_INTERNAL
FGL_API FglState RenderState;
#endif

typedef bool(*FglVertexFunc)(FglState* State, vec3 Vert);
typedef bool(*FglFragmentFunc)(FglState* State, vec2 UV, FglColor* OutColor);

// Initialization and state
FGL_API FGL_INLINE void fglInit(void* VideoMemory, int32_t Width, int32_t Height, int32_t BPP, int32_t Stride, PixelOrder Order);
FGL_API FGL_INLINE FglState* fglGetState();
FGL_API FGL_INLINE void fglSetState(FglState* State);

// Shaders
FGL_API FGL_INLINE void fglBindShader(void* Shader, FglShaderType ShaderType);

// Buffer functions and textures
FGL_API FGL_INLINE FglBuffer fglCreateBuffer(void* Memory, int32_t Width, int32_t Height);
FGL_API FGL_INLINE FglBuffer fglCreateBufferFromPng(void* PngInMemory, int32_t Len);
FGL_API FGL_INLINE void fglDisplayToFramebuffer(FglBuffer* Buffer);
FGL_API FGL_INLINE void fglClearBuffer(FglBuffer* Buffer, FglColor Clr);
FGL_API FGL_INLINE void fglBindTexture(FglBuffer* TextureBuffer, int32_t Slot);

// Drawing
FGL_API FGL_INLINE void fglDrawLine(FglBuffer* Buffer, FglColor Color, int32_t X0, int32_t Y0, int32_t X1, int32_t Y1);
FGL_API FGL_INLINE void fglDrawTriangle3(FglBuffer* Buffer, FglColor Color, FglTriangle3* Tri);
FGL_API FGL_INLINE void fglFillTriangle3(FglBuffer* Buffer, FglColor Color, FglTriangle3* Tri);
FGL_API FGL_INLINE void fglRenderTriangle3(FglBuffer* Buffer, FglTriangle3* Tri, FglTriangle2* UV);

// Util functions, do not export these
#ifdef FGL_INTERNAL
FGL_INLINE void fglBoundingBox(FglTriangle3* Tri, vec3 Min, vec3 Max);
FGL_INLINE void fglBoundingRect(FglTriangle3* Tri, vec2 Min, vec2 Max);
FGL_INLINE bool fglBarycentric(FglTriangle3* Tri, int32_t X, int32_t Y, vec3 Val);
FGL_INLINE void fglBlend(FglColor Src, FglColor* Dst);
#endif