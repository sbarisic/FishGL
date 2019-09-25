#include "soft_render.cl"

__kernel void _main(const int Width, const int Height, const int VertexCount, __global Color* Out, __global Vec3* Verts) {
	int x = get_global_id(0);
	int y = get_global_id(1);

	if (x < 0 || y < 0 || x >= Width || y >= Height)
		return;

	Vec4 AClr = vec4(1, 0, 0, 1);
	Vec4 BClr = vec4(0, 1, 0, 1);
	Vec4 CClr = vec4(0, 0, 1, 1);

	Vec2 AUV = vec2(0, 0);
	Vec2 BUV = vec2(0, 0);
	Vec2 CUV = vec2(0, 0);

	for (int i = 0; i < VertexCount; i += 3) {
		Vec3 A = vert_main(Verts[i + 0]);
		Vec3 B = vert_main(Verts[i + 1]);
		Vec3 C = vert_main(Verts[i + 2]);

		Vec3 Bar = Barycentric(x, y, A, B, C);
		if (BaryOutside(Bar))
			continue;

		Vec2 UV = Vec2Interpolate(AUV, BUV, CUV, Bar);
		Vec4 Clr = Vec4Interpolate(AClr, BClr, CClr, Bar);

		Out[y * Width + x] = colorf(Clr);
	}
}
