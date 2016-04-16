using UnityEngine;
using System.Collections;
using System.Text;

public class MeshManipulation : MonoBehaviour
{
	public string seed = "test";
	public float salt = 0.5234f;
	public bool terraforming = true;
	public bool smoothing = true;
	public bool gauss = true;
	public bool flatshading = true;
	public float sigma = 0.4f;
	public int dimensions = 11;
	public int smoothWidth = 1;

	private char[] psRand;
	private Vector3[] verts;
	private int[] triangles;

	// Use this for initialization
	void Awake ()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		verts = mesh.vertices;
		triangles = mesh.triangles;

		// Generate 'random' string with seed
		psRand = deviate (verts.Length);

		if (terraforming) calcTerraform ();

		// 1 <= size <= 4 smoothing
		if (smoothing && !gauss) calcMean (smoothWidth, dimensions);

		// 0.2 <= sigma <= 0.3 roughen
		// 0.3 <= sigma <= 2.0 smoothing
		if (smoothing && gauss) calcGauss (sigma, dimensions);

		if (flatshading) calcFlatShading ();
		
		mesh.vertices = verts;
		mesh.triangles = triangles;
		mesh.RecalculateNormals ();

		// modify mesh collider
		GetComponent<MeshCollider> ().sharedMesh = mesh;
	}

	// Update is called once per frame
	void Update ()
	{
	}

	char[] deviate (int length)
	{
		StringBuilder md5 = new StringBuilder (Utility.Md5Sum (seed));

		while (md5.Length < length)
		{
			md5.Append (Utility.Md5Sum (md5.ToString ()));
		}
		return md5.ToString ().Substring (0, length).ToCharArray ();
	}

	void calcTerraform ()
	{
		// modulize heights of vertices
		for (int i = 0; i < verts.Length; i++)
		{
			verts[i].y += (((float)psRand[i]) % salt);
		}
	}

	void calcFlatShading() {
		Vector3[] newVerts = new Vector3[triangles.Length];
		// split triangles
		for (int i = 0; i < triangles.Length; i++)
		{
			newVerts[i] = verts[triangles[i]];
			triangles[i] = i;
		}
		verts = newVerts;
	}

	void reduceVertices (float scale)
	{
		int newLength = Mathf.RoundToInt (((float)verts.Length) * scale);
		Vector3[] newVerts = new Vector3[newLength];

		int j = 0;
		for (int i = 0; i < verts.Length; i++)
		{
			if (verts[i] != new Vector3())
			{
				newVerts[j++] = verts[i];
			}
		}
	}

	void calcMean (int size, int dim) {
		int width = (2 * size) + 1;
		Vector3[] newVerts = verts;

		for (int i = 0; i < verts.Length; i++)
		{
			newVerts[i].y = 0.0f;
			for (int j = -size; j <= size; j ++)
			{
				for (int k = -size; k <= size; k++)
				{
					int pos = i + (j * dim) + k;
					if (pos >= 0 && pos < verts.Length
					    && (Mathf.Floor (i / dim) == Mathf.Floor (pos / dim)
					    || i % dim == pos % dim))
					{
						newVerts[i].y += verts[pos].y;
					}
				}
			}
			newVerts[i].y /= Mathf.Pow((float) width, 2.0f);
		}
		verts = newVerts;
	}

	void calcGauss(float sigma, int dim) {
		int width = (int) (6 * Mathf.Sqrt(2 * sigma));
		float[] matrix = new float[(2 * width) + 1];
		Vector3[] tmpVerts = (Vector3[]) verts.Clone ();
//		StringBuilder log = new StringBuilder();

		for (int i = 0; i < (2 * width) + 1; i++)
		{
			matrix[i] = 1.0f / (Mathf.Sqrt(2.0f * Mathf.PI) * sigma)
					* Mathf.Exp(-(Mathf.Pow((float)(i - width), 2.0f))
					/ (2.0f * Mathf.Pow(sigma, 2.0f)));
		}

		for (int i = 0; i < verts.Length; i++)
		{
			tmpVerts[i].y = 0.0f;
            for (int j = -width; j <= width; j++)
			{
				int pos = i + j;
				if (pos >= 0 && pos < verts.Length
				    && Mathf.Floor (i / dim) == Mathf.Floor (pos / dim))
				{
					tmpVerts[i].y += verts[pos].y * matrix[j + width];
				}
			}
//			log.Append (tmpVerts[i].y.ToString()+ " ");
		}
//		print (log);

		Vector3[] newVerts = (Vector3[]) verts.Clone ();

		for (int i = 0; i < verts.Length; i++)
		{
			newVerts[i].y = 0.0f;
			for (int j = -width; j <= width; j++)
			{
				int pos = i + (j * dim);
				if (pos >= 0 && pos < verts.Length)
				{
					newVerts[i].y += tmpVerts[pos].y * matrix[j + width];
				}
			}
		}
		
		verts = newVerts;
	}
}
