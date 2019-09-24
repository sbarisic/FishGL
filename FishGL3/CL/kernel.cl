#include "soft_render.cl"

__kernel void main(__constant FGLGlobal* Global, __global Color* Out, __constant Triangle* Tris) {
	int x = get_global_id(0);
	int y = get_global_id(1);

	if (x < 0 || y < 0 || x >= Global->Width || y >= Global->Height)
		return;

	Vec4 AClr = vec4(1, 0, 0, 1);
	Vec4 BClr = vec4(0, 1, 0, 1);
	Vec4 CClr = vec4(0, 0, 1, 1);

	Vec2 AUV = vec2(0, 0);
	Vec2 BUV = vec2(0, 0);
	Vec2 CUV = vec2(0, 0);

	for (int32 i = 0; i < Global->TriCount; i++) {
		Vec3 A = vert_main(Tris[i].A);
		Vec3 B = vert_main(Tris[i].B);
		Vec3 C = vert_main(Tris[i].C);

		Vec3 Bar = Barycentric(x, y, A, B, C);
		if (BaryOutside(Bar))
			continue;

		Vec2 UV = Vec2Interpolate(AUV, BUV, CUV, Bar);
		Vec4 Clr = Vec4Interpolate(AClr, BClr, CClr, Bar);

		Out[y * Global->Width + x] = colorf(Clr);
	}
}
