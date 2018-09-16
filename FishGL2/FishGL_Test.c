#include <FishGL.h>
#include <FishGLShader.h>
#include <math.h>

#define rnd_num(min, max) ((rand() % (max - min)) + min)

int Width;
int Height;

FglBuffer ColorBuffer;
FglBuffer TestTex;
FglBuffer Test2Tex;

FglColor Red;
FglColor Green;
FglColor Blue;
FglColor White;

#define TRI_COUNT 2
FglTriangle3 Tri[TRI_COUNT];
FglTriangle2 UV[TRI_COUNT];

void* LoadFile(const char* FileName, int32_t* Len) {
	FILE* F;

	fopen_s(&F, FileName, "rb");
	fseek(F, 0, SEEK_END);
	*Len = ftell(F);
	rewind(F);

	void* Buffer = calloc(1, *Len + 1);
	fread(Buffer, *Len, 1, F);
	fclose(F);

	return Buffer;
}

void InitShit() {
	White = (FglColor) { 255, 255, 255, 255 };
	Red = (FglColor) { 255, 0, 0, 255 };
	Green = (FglColor) { 0, 255, 0, 255 };
	Blue = (FglColor) { 0, 0, 255, 255 };
}

bool VertexShader(FglState* State, vec3 Vert) {
	FglColor Clr = Red;

	if (State->VertNum == 1)
		Clr = Green;
	else if (State->VertNum == 2)
		Clr = Blue;

	FglVarying* V = fglShaderGetVarying(1);
	V->Vec3[XElement] = Clr.R;
	V->Vec3[YElement] = Clr.G;
	V->Vec3[ZElement] = Clr.B;

	return FGL_KEEP;
}

bool FragmentShader(FglState* State, vec2 UV, FglColor* OutColor) {
	FglColor C = fglShaderSampleTextureUV(&State->Textures[0], UV);

	vec3 C2;
	glm_vec_copy(fglShaderGetVarying(1)->Vec3, C2);

	C.R = (int)(C.R * (C2[XElement] / 255));
	C.G = (int)(C.G * (C2[YElement] / 255));
	C.B = (int)(C.B * (C2[ZElement] / 255));

	*OutColor = C;

	return FGL_KEEP;
}

__declspec(dllexport) void fglDebugInit(int W, int H, int BPP) {
	int32_t Len = 0;

	Width = W;
	Height = H;
	InitShit();

	ColorBuffer = fglCreateBuffer(malloc(W * H * sizeof(FglColor)), W, H);

	TestTex = fglCreateBufferFromPng(LoadFile("test.png", &Len), Len);
	Test2Tex = fglCreateBufferFromPng(LoadFile("test2.png", &Len), Len);

	fglBindTexture(&TestTex, 0);
	//fglBindShader(&VertexShader, FglShaderType_Vertex);
	//fglBindShader(&FragmentShader, FglShaderType_Fragment);

	float X = 100;
	float Y = 100;
	float SX = 1366 - 200;
	float SY = 768 - 200;

	Tri[0] = (FglTriangle3) { { X, Y, 0 }, { X + SX, Y, 0 }, { X, Y + SY, 0 } };
	UV[0] = (FglTriangle2) { {0.0f, 0.0f}, { 1.0f, 0.0f }, { 0.0f, 1.0f } };

	Tri[1] = (FglTriangle3) { { X + SX, Y, 0}, { X + SX, Y + SY, 0 }, { X, Y + SY, 0 } };
	UV[1] = (FglTriangle2) { {1.0f, 0.0f}, { 1.0f, 1.0f }, { 0.0f, 1.0f } };
}

__declspec(dllexport) void fglDebugLoop() {
	fglClearBuffer(&ColorBuffer, (FglColor) { 50, 60, 80, 255 });

	for (size_t i = 0; i < TRI_COUNT; i++)
		fglRenderTriangle3(&ColorBuffer, &Tri[i], &UV[i]);

	fglDisplayToFramebuffer(&ColorBuffer);
}