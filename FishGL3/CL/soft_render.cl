#pragma once

#define SWAP_INT(a, b) do { int aa = a; a = b; b = aa; } while (false)

typedef unsigned char byte;
typedef int int32;

typedef struct {
	int32 Width;
	int32 Height;

	int32 TriCount;
} FGLGlobal;

typedef struct {
	byte R;
	byte G;
	byte B;
} Color;

typedef struct {
	float X;
	float Y;
} Vec2;

Vec2 vec2(float X, float Y) {
	return (Vec2) { X, Y };
}

typedef struct {
	float X;
	float Y;
	float Z;
} Vec3;

Vec3 vec3(float X, float Y, float Z) {
	return (Vec3) { X, Y, Z };
}

typedef struct {
	float X;
	float Y;
	float Z;
	float W;
} Vec4;

Vec4 vec4(float X, float Y, float Z, float W) {
	return (Vec4) { X, Y, Z, W };
}

typedef struct {
	Vec3 A;
	Vec3 B;
	Vec3 C;
} Triangle;

Vec3 Vec3Add(Vec3 A, Vec3 B) {
	A.X += B.X;
	A.Y += B.Y;
	A.Z += B.Z;
	return A;
}

Vec3 Vec3Sub(Vec3 A, Vec3 B) {
	A.X -= B.X;
	A.Y -= B.Y;
	A.Z -= B.Z;
	return A;
}

Vec3 Vec3Mul(Vec3 A, Vec3 B) {
	A.X *= B.X;
	A.Y *= B.Y;
	A.Z *= B.Z;
	return A;
}

float Vec3Dot(Vec3 A, Vec3 B) {
	Vec3 R = Vec3Mul(A, B);
	return R.X + R.Y + R.Z;
}

Vec3 Vec3Cross(Vec3 A, Vec3 B) {
	return (Vec3) { A.Y * B.Z - A.Z * B.Y, A.Z * B.X - A.X * B.Z, A.X * B.Y - A.Y * B.X };
}

Vec3 Barycentric(int PX, int PY, Vec3 A, Vec3 B, Vec3 C) {
	Vec3 V0 = (Vec3) { C.X - A.X, B.X - A.X, A.X - PX };
	Vec3 V1 = (Vec3) { C.Y - A.Y, B.Y - A.Y, A.Y - PY };
	Vec3 U = Vec3Cross(V0, V1);

	if (fabs(U.Z) < 1)
		return (Vec3) { -1, 0, 0 };

	float X = 1.0f - (U.X + U.Y) / U.Z;
	float Y = U.Y / U.Z;
	float Z = U.X / U.Z;
	return (Vec3) { X, Y, Z };
}

bool BaryOutside(Vec3 Bar) {
	return (Bar.X < 0 || Bar.Y < 0 || Bar.Z < 0);
}

Vec3 Vec3Interpolate(Vec3 A, Vec3 B, Vec3 C, Vec3 Bar) {
	float X = (A.X * Bar.X) + (B.X * Bar.Y) + (C.X * Bar.Z);
	float Y = (A.Y * Bar.X) + (B.Y * Bar.Y) + (C.Y * Bar.Z);
	float Z = (A.Z * Bar.X) + (B.Z * Bar.Y) + (C.Z * Bar.Z);
	return (Vec3) { X, Y, Z };
}

Vec2 Vec2Interpolate(Vec2 A, Vec2 B, Vec2 C, Vec3 Bar) {
	float X = (A.X * Bar.X) + (B.X * Bar.Y) + (C.X * Bar.Z);
	float Y = (A.Y * Bar.X) + (B.Y * Bar.Y) + (C.Y * Bar.Z);
	return (Vec2) { X, Y };
}


Vec3 vert_main(Vec3 glPosition);
Vec4 frag_main(Vec2 glFragCoord);