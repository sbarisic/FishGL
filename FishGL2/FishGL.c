#define FGL_INTERNAL
#define FGL_IMPLEMENTATION
#include <FishGL.h>
#include <FishGLShader.h>

//#define fgl_fminf fminf
//#define fgl_fmaxf fmaxf

#define fgl_fminf(a, b) ((a) < (b) ? (a) : (b))
#define fgl_fmaxf(a, b) ((a) < (b) ? (b) : (a))

FGL_INLINE void fglInit(void* VideoMemory, int32_t Width, int32_t Height, int32_t BPP, int32_t Stride, PixelOrder Order) {
	memset(&RenderState, 0, sizeof(FglState));

	RenderState.VideoMemory = VideoMemory;
	RenderState.Width = Width;
	RenderState.Height = Height;
	RenderState.BPP = BPP;
	RenderState.Stride = Stride;
	RenderState.Order = Order;

	RenderState.FragmentShader = NULL;
	RenderState.VertexShader = NULL;

	RenderState.BorderColor = (FglColor) { 0, 0, 0, 255 };
	RenderState.TextureWrap = FglTextureWrap_BorderColor;
	RenderState.BlendMode = FglBlendMode_None;
}

FGL_INLINE FglState* fglGetState() {
	return &RenderState;
}

FGL_INLINE void fglSetState(FglState* State) {
	RenderState = *State;
}

// Shaders 

FGL_INLINE void fglBindShader(void* Shader, FglShaderType ShaderType) {
	if (ShaderType == FglShaderType_Fragment)
		RenderState.FragmentShader = (FglFragmentFunc)Shader;
	else if (ShaderType == FglShaderType_Vertex)
		RenderState.VertexShader = (FglVertexFunc)Shader;
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

FGL_INLINE FglBuffer fglCreateBufferFromPng(void* PngInMemory, int32_t Len) {
	int32_t X, Y, Comp;
	FglBuffer Buffer;
	stbi_uc* Data;

	memset(&Buffer, 0, sizeof(FglBuffer));

	if ((Data = stbi_load_from_memory((stbi_uc*)PngInMemory, Len, &X, &Y, &Comp, 4))) {
		Buffer.Memory = (void*)Data;
		Buffer.Width = X;
		Buffer.Height = Y;
		Buffer.Length = X * Y * 4;
		Buffer.PixelCount = X * Y;
	}

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

FGL_INLINE void fglBindTexture(FglBuffer* TextureBuffer, int32_t Slot) {
	RenderState.Textures[Slot] = *TextureBuffer;
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

FGL_INLINE void fglDrawTriangle3(FglBuffer* Buffer, FglColor Color, FglTriangle3* Tri) {
	fglDrawLine(Buffer, Color, (int32_t)Tri->A[XElement], (int32_t)Tri->A[YElement], (int32_t)Tri->B[XElement], (int32_t)Tri->B[YElement]);
	fglDrawLine(Buffer, Color, (int32_t)Tri->A[XElement], (int32_t)Tri->A[YElement], (int32_t)Tri->C[XElement], (int32_t)Tri->C[YElement]);
	fglDrawLine(Buffer, Color, (int32_t)Tri->B[XElement], (int32_t)Tri->B[YElement], (int32_t)Tri->C[XElement], (int32_t)Tri->C[YElement]);
}

FGL_INLINE void fglFillTriangle3(FglBuffer* Buffer, FglColor Color, FglTriangle3* Tri) {
	vec2 Min, Max;
	vec3 V;
	fglBoundingRect(Tri, Min, Max);

	for (size_t y = (size_t)Min[YElement]; y < Max[YElement]; y++)
		for (size_t x = (size_t)Min[XElement]; x < Max[XElement]; x++) {
			if (fglBarycentric(Tri, x, y, V))
				Buffer->Pixels[y * Buffer->Width + x] = Color;
		}
}

FGL_INLINE void fglRenderTriangle3(FglBuffer* Buffer, FglTriangle3* TriangleIn, FglTriangle2* UVsIn) {
	if (UVsIn != NULL) {
		memcpy(&RenderState.VarIn[0].A.Vec2, &UVsIn->A, sizeof(vec2));
		memcpy(&RenderState.VarIn[0].B.Vec2, &UVsIn->B, sizeof(vec2));
		memcpy(&RenderState.VarIn[0].C.Vec2, &UVsIn->C, sizeof(vec2));
	}

	FglTriangle3 Tri = *TriangleIn;

	if (RenderState.VertexShader != NULL) {
		FglVertexFunc VertShader = (FglVertexFunc)RenderState.VertexShader;
		RenderState.CurShader = FglShaderType_Vertex;

		RenderState.VertNum = 0;
		if (VertShader(&RenderState, Tri.A) == FGL_DISCARD)
			return;

		RenderState.VertNum = 1;
		if (VertShader(&RenderState, Tri.B) == FGL_DISCARD)
			return;

		RenderState.VertNum = 2;
		if (VertShader(&RenderState, Tri.C) == FGL_DISCARD)
			return;
	}

	vec2 Min, Max;
	fglBoundingRect(&Tri, Min, Max);

	for (size_t y = (size_t)Min[YElement]; y < Max[YElement]; y++)
		for (size_t x = (size_t)Min[XElement]; x < Max[XElement]; x++) {
			vec3 Barycentric;
			if (fglBarycentric(&Tri, x, y, Barycentric)) {
				glm_mat3_mulv(&RenderState.VarIn[0].Mat, Barycentric, &RenderState.VarOut[0].Vec3);

				if (RenderState.FragmentShader != NULL) {
					for (size_t i = 1; i < FGL_VARYING_COUNT; i++)
						glm_mat3_mulv(&RenderState.VarIn[i].Mat, Barycentric, &RenderState.VarOut[i].Vec3);

					FglColor OutClr;
					FglFragmentFunc FragShader = (FglFragmentFunc)RenderState.FragmentShader;
					RenderState.CurShader = FglShaderType_Fragment;

					if (FragShader(&RenderState, RenderState.VarOut[0].Vec2, &OutClr) == FGL_DISCARD)
						continue;

					fglBlend(OutClr, &Buffer->Pixels[y * Buffer->Width + x]);
				}
				else
					fglBlend(fglShaderSampleTextureUV(&RenderState.Textures[0], RenderState.VarOut[0].Vec2), &Buffer->Pixels[y * Buffer->Width + x]);
			}

		}
}

// Util functions

FGL_INLINE void fglBoundingBox(FglTriangle3* Tri, vec3 Min, vec3 Max) {
	Min[XElement] = fgl_fminf(fgl_fminf(Tri->A[XElement], Tri->B[XElement]), Tri->C[XElement]);
	Min[YElement] = fgl_fminf(fgl_fminf(Tri->A[YElement], Tri->B[YElement]), Tri->C[YElement]);
	Min[ZElement] = fgl_fminf(fgl_fminf(Tri->A[ZElement], Tri->B[ZElement]), Tri->C[ZElement]);

	Max[XElement] = fgl_fmaxf(fgl_fmaxf(Tri->A[XElement], Tri->B[XElement]), Tri->C[XElement]);
	Max[YElement] = fgl_fmaxf(fgl_fmaxf(Tri->A[YElement], Tri->B[YElement]), Tri->C[YElement]);
	Max[ZElement] = fgl_fmaxf(fgl_fmaxf(Tri->A[ZElement], Tri->B[ZElement]), Tri->C[ZElement]);
}

FGL_INLINE void fglBoundingRect(FglTriangle3* Tri, vec2 Min, vec2 Max) {
	Min[XElement] = fgl_fminf(fgl_fminf(Tri->A[XElement], Tri->B[XElement]), Tri->C[XElement]);
	Min[YElement] = fgl_fminf(fgl_fminf(Tri->A[YElement], Tri->B[YElement]), Tri->C[YElement]);

	Max[XElement] = fgl_fmaxf(fgl_fmaxf(Tri->A[XElement], Tri->B[XElement]), Tri->C[XElement]);
	Max[YElement] = fgl_fmaxf(fgl_fmaxf(Tri->A[YElement], Tri->B[YElement]), Tri->C[YElement]);
}

FGL_INLINE bool fglBarycentric(FglTriangle3* Tri, int32_t X, int32_t Y, vec3 Val) {
	vec3 U;
	vec3 a;
	vec3 b;

	a[XElement] = (Tri->C[XElement]) - (Tri->A[XElement]);
	a[YElement] = (Tri->B[XElement]) - (Tri->A[XElement]);
	a[ZElement] = (Tri->A[XElement]) - (float)X;

	b[XElement] = (Tri->C[YElement]) - (Tri->A[YElement]);
	b[YElement] = (Tri->B[YElement]) - (Tri->A[YElement]);
	b[ZElement] = (Tri->A[YElement]) - (float)Y;

	glm_vec_cross(a, b, U);

	if (fabsf(U[ZElement]) < 1)
		return false;

	Val[XElement] = 1.0f - ((U[XElement] + U[YElement]) / U[ZElement]);
	Val[YElement] = U[YElement] / U[ZElement];
	Val[ZElement] = U[XElement] / U[ZElement];

	if (Val[XElement] < 0 || Val[YElement] < 0 || Val[ZElement] < 0)
		return false;

	return true;
}

FGL_INLINE void fglBlend(FglColor Src, FglColor* Dst) {
	if (RenderState.BlendMode == FglBlendMode_None)
		*Dst = Src;
	else
		*Dst = (FglColor) { 255, 50, 220, 255 };
}