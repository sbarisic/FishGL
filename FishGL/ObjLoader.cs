using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;using System.Globalization;

namespace FishGL {
	static class ObjLoader {
		static void ParseFaceElement(string Element, out int VertInd) {
			string[] ElementTokens = Element.Trim().Split('/');

			VertInd = int.Parse(ElementTokens[0]);
		}

		static void ParseFace(string[] Tokens, out int[] VertInds) {
			VertInds = new int[Tokens.Length];
			for (int i = 0; i < VertInds.Length; i++) {
				int VertInd;
				ParseFaceElement(Tokens[i], out VertInd);
				VertInds[i] = VertInd;
			}
		}

		static float ParseFloat(string Str) {
			return float.Parse(Str, CultureInfo.InvariantCulture);
		}

		public static void Load(string[] Lines, out Vector3[][] Triangles) {
			List<Vector3> Verts = new List<Vector3>();
			List<Vector3[]> Tris = new List<Vector3[]>();

			for (int i = 0; i < Lines.Length; i++) {
				string L = Lines[i].ToLower().Trim();
				if (L.Length == 0 || L.StartsWith("#"))
					continue;

				string[] Tokens = L.Split(' ');


				switch (Tokens[0]) {
					case "v": {
							Verts.Add(new Vector3(ParseFloat(Tokens[1]), ParseFloat(Tokens[2]), ParseFloat(Tokens[3])));
							break;
						}

					case "vt": { // Texture coords
							break;
						}

					case "vn": { // Vertex normals
							break;
						}

					case "f": { // Face
							int[] VertInds;
							ParseFace(Tokens.Skip(1).ToArray(), out VertInds);

							/*Tris.Add(Verts[VertInds[0] - 1]);
							Tris.Add(Verts[VertInds[1] - 1]);
							Tris.Add(Verts[VertInds[2] - 1]);*/

							Tris.Add(new Vector3[] { Verts[VertInds[0] - 1], Verts[VertInds[1] - 1], Verts[VertInds[2] - 1] });
							break;
						}

					default:
						//Console.WriteLine("Unknown obj type: {0}", Tokens[0]);
						break;
				}
			}

			Triangles = Tris.ToArray();
		}
	}
}
