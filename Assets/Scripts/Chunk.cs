using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
	public byte[,,] map;

	public Color upFaceColor;
	public Color downFaceColor;
	public Color rightFaceColor;
	public Color leftFaceColor;
	public Color frontFaceColor;
	public Color backFaceColor;

	public Color shadeColor;

	World world;

	Mesh mesh;
	Mesh coll_mesh;

	List<Vector3> verts = new List<Vector3>();
	List<Color> colors = new List<Color>();
	List<int> tris = new List<int>();
	List<Vector2> uv = new List<Vector2>();

	List<Vector3> coll_verts = new List<Vector3>();
	List<int> coll_tris = new List<int>();

	[HideInInspector]
	public bool initialized = false;

	int size;

	bool rising;
	float riseSpeed;
	public float curYRisePos = 0;

	public void Start()
	{
		world = World.Instance;

		size = World.Instance.chunkSize;
		map = new byte[size, size, size];

		int chunkX = Mathf.RoundToInt(transform.position.x);
		int chunkY = Mathf.RoundToInt(transform.position.y);
		int chunkZ = Mathf.RoundToInt(transform.position.z);

		for(int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				for (int z = 0; z < size; z++)
				{
					byte block = world.GetBlock(chunkX + x, chunkY + y, chunkZ + z);
					map[x, y, z] = block;
				}
			}
		}

		initialized = true;

		Regenerate();
	}

	public void CloseMeshTarget()
	{
		mesh.vertices = verts.ToArray();
		mesh.triangles = tris.ToArray();
		mesh.uv = uv.ToArray();
		mesh.SetColors(colors);

		coll_mesh.vertices = coll_verts.ToArray();
		coll_mesh.triangles = coll_tris.ToArray();

		coll_mesh.RecalculateBounds();
		coll_mesh.RecalculateNormals();
		coll_mesh.RecalculateTangents();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		GetComponent<MeshCollider>().sharedMesh = coll_mesh;
	}

	public void CreateMeshTarget()
	{
		mesh = new Mesh();
		coll_mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		verts.Clear();
		tris.Clear();
		uv.Clear();
		colors.Clear();

		coll_verts.Clear();
		coll_tris.Clear();
	}

	public void UpdateCollisions()
	{
		if (transform.Find("collisions"))
		{
			Destroy(transform.Find("collisions").gameObject);
		}

		GameObject newCollisions = new GameObject();
		newCollisions.name = "collisions";
		newCollisions.transform.SetParent(transform);
		newCollisions.transform.localPosition = Vector3.zero;
		newCollisions.tag = "Chunk";

		for (int x = 0; x < world.chunkSize; x++)
		{
			for (int y = 0; y < world.chunkSize; y++)
			{
				for (int z = 0; z < world.chunkSize; z++)
				{
					//int x1 = x + Mathf.RoundToInt(transform.position.x);
					//int y1 = y + Mathf.RoundToInt(transform.position.y);
					//int z1 = z + Mathf.RoundToInt(transform.position.z);

					if (BlockRequiresCollider(x, y, z) && map[x, y, z] < 29 && map[x, y, z] != 0)
					{
						BoxCollider box = newCollisions.AddComponent<BoxCollider>();
						box.center = new Vector3(x + 0.5f, y + 0.5f, z - 0.5f);
					}
				}
			}
		}
	}

	public void DrawBlock(int x, int y, int z, byte block)
	{
		Vector3 start = new Vector3(x, y, z);
		Vector3 offset1, offset2;

		if (block < 29)
		{
			if (IsAirBlock(x, y - 1, z))
			{
				offset1 = Vector3.left;
				offset2 = Vector3.back;
				DrawFace(start + Vector3.right, offset1, offset2, block);
			}
			if (IsAirBlock(x, y + 1, z))
			{
				offset1 = Vector3.right;
				offset2 = Vector3.back;
				DrawFace(start + Vector3.up, offset1, offset2, block);
			}

			if (IsAirBlock(x - 1, y, z))
			{
				offset1 = Vector3.up;
				offset2 = Vector3.back;
				DrawFace(start, offset1, offset2, block);
			}

			if (IsAirBlock(x + 1, y, z))
			{
				offset1 = Vector3.down;
				offset2 = Vector3.back;
				DrawFace(start + Vector3.right + Vector3.up, offset1, offset2, block);
			}

			if (IsAirBlock(x, y, z - 1))
			{
				offset1 = Vector3.left;
				offset2 = Vector3.up;
				DrawFace(start + Vector3.right + Vector3.back, offset1, offset2, block);
			}

			if (IsAirBlock(x, y, z + 1))
			{
				offset1 = Vector3.right;
				offset2 = Vector3.up;
				DrawFace(start, offset1, offset2, block);
			}
		}
		else
		{
			DrawFace(start, Vector3.up, new Vector3(1, 0, -1), block);
			DrawFace(start, new Vector3(1, 0, -1), Vector3.up, block);
			DrawFace(start + Vector3.right, Vector3.up, new Vector3(-1, 0, -1), block);
			DrawFace(start + Vector3.right, new Vector3(-1, 0, -1), Vector3.up, block);
		}
	}

	public void DrawFace(Vector3 start, Vector3 offset1, Vector3 offset2, byte block)
	{
		int x = Mathf.RoundToInt(start.x) + Mathf.RoundToInt(transform.position.x);
		int y = Mathf.RoundToInt(start.y) + Mathf.RoundToInt(transform.position.y);
		int z = Mathf.RoundToInt(start.z) + Mathf.RoundToInt(transform.position.z);

		int index = verts.Count;
		int coll_index = coll_verts.Count;

		verts.Add(start);
		verts.Add(start + offset1);
		verts.Add(start + offset2);
		verts.Add(start + offset1 + offset2);

		if(block < 29)
		{
			coll_verts.Add(start);
			coll_verts.Add(start + offset1);
			coll_verts.Add(start + offset2);
			coll_verts.Add(start + offset1 + offset2);
		}

		Vector2 uvBase;
		Vector2 uvBaseTop = Vector2.one * 2;
		Vector2 uvBaseBottom = Vector2.one * 2;

		switch (block)
		{
			default:
				uvBase = new Vector2(0, 0.875f);
				break;
			case 2:
				uvBase = new Vector2(0.125f, 0.875f);
				uvBaseTop = new Vector2(0.25f, 0.875f);
				uvBaseBottom = new Vector2(0, 0.875f);
				break;
			case 3:
				uvBase = new Vector2(0.375f, 0.875f);
				break;
			case 4:
				uvBase = new Vector2(0.5f, 0.875f);
				break;
			case 5:
				uvBase = new Vector2(0.625f, 0.875f);
				break;
			case 6:
				uvBase = new Vector2(0.75f, 0.875f);
				break;
			case 7:
				uvBase = new Vector2(0.875f, 0.875f);
				break;
			case 8:
				uvBase = new Vector2(0, 0.75f);
				break;
			case 9:
				uvBase = new Vector2(0.125f, 0.75f);
				break;
			case 10:
				uvBase = new Vector2(0.25f, 0.75f);
				break;
			case 11:
				uvBase = new Vector2(0.375f, 0.75f);
				break;
			case 12:
				uvBase = new Vector2(0.5f, 0.75f);
				break;
			case 13:
				uvBase = new Vector2(0.75f, 0.75f);
				uvBaseTop = new Vector2(0.625f, 0.75f);
				break;
			case 14:
				uvBase = new Vector2(0.875f, 0.75f);
				break;
			case 15:
				uvBase = new Vector2(0, 0.625f);
				break;
			case 16:
				uvBase = new Vector2(0.125f, 0.625f);
				break;
			case 17:
				uvBase = new Vector2(0.25f, 0.625f);
				break;
			case 18:
				uvBase = new Vector2(0.375f, 0.625f);
				break;
			case 19:
				uvBase = new Vector2(0.5f, 0.625f);
				break;
			case 20:
				uvBase = new Vector2(0.625f, 0.625f);
				break;
			case 21:
				uvBase = new Vector2(0.75f, 0.625f);
				uvBaseTop = new Vector2(0, 0.625f);
				break;
			case 22:
				uvBase = new Vector2(0.875f, 0.625f);
				break;
			case 23:
				uvBase = new Vector2(0, 0.5f);
				break;
			case 24:
				uvBase = new Vector2(0.125f, 0.5f);
				break;
			case 25:
				uvBase = new Vector2(0.25f, 0.5f);
				break;
			case 26:
				uvBase = new Vector2(0.375f, 0.5f);
				break;
			case 27:
				uvBase = new Vector2(0.625f, 0.5f);
				uvBaseTop = new Vector2(0.5f, 0.5f);
				break;
			case 28:
				uvBase = new Vector2(0.875f, 0.5f);
				break;
			case 29:
				uvBase = new Vector2(0, 0.375f);
				break;
			case 30:
				uvBase = new Vector2(0.125f, 0.375f);
				break;
			case 31:
				uvBase = new Vector2(0.25f, 0.375f);
				break;
			case 32:
				uvBase = new Vector2(0.375f, 0.375f);
				break;
			case 33:
				uvBase = new Vector2(0.5f, 0.375f);
				break;
			case 34:
				uvBase = new Vector2(0.625f, 0.375f);
				break;
			case 35:
				uvBase = new Vector2(0.875f, 0.375f);
				break;
		}

		if (uvBaseTop.x == 2 && uvBaseTop.y == 2) uvBaseTop = uvBase;

		if (uvBaseBottom.x == 2 && uvBaseBottom.y == 2)
			uvBaseBottom = uvBaseTop;

		if (block < 29)
		{
			if (offset1 == Vector3.right && offset2 == Vector3.up)//front face
			{
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left

				Color color = frontFaceColor;
				if (world.BlockIsShadedAt(x, y, z + 1)) color -= shadeColor;

				color.a = 1;

				for (int i = 0; i < 4; i++)
					colors.Add(color);
			}
			else if (offset1 == Vector3.left && offset2 == Vector3.up)//back face
			{
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left

				Color color = backFaceColor;
				if (world.BlockIsShadedAt(x - 1, y, z)) color -= shadeColor;

				color.a = 1;

				for (int i = 0; i < 4; i++)
					colors.Add(color);
			}
			else if (offset1 == Vector3.down && offset2 == Vector3.back)//right face
			{
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left
				uv.Add(uvBase);//bottom left

				Color color = rightFaceColor;
				if (world.BlockIsShadedAt(x, y - 1, z)) color -= shadeColor;

				color.a = 1;

				for (int i = 0; i < 4; i++)
					colors.Add(color);
			}
			else if (offset1 == Vector3.up && offset2 == Vector3.back)//left face
			{
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right

				Color color = leftFaceColor;
				if (world.BlockIsShadedAt(x - 1, y, z)) color -= shadeColor;

				color.a = 1;

				for (int i = 0; i < 4; i++)
					colors.Add(color);
			}
			else
			{
				if (offset1 == Vector3.left && offset2 == Vector3.back)
				{
					uv.Add(uvBaseBottom + new Vector2(0, 0.125f));//top left
					uv.Add(uvBaseBottom + new Vector2(0.125f, 0.125f));//top right
					uv.Add(uvBaseBottom);//bottom left
					uv.Add(uvBaseBottom + new Vector2(0.125f, 0));//bottom right

					Color color = downFaceColor;

					color.a = 1;

					for (int i = 0; i < 4; i++)
						colors.Add(color);
				}
				else
				{
					uv.Add(uvBaseTop + new Vector2(0, 0.125f));//top left
					uv.Add(uvBaseTop + new Vector2(0.125f, 0.125f));//top right
					uv.Add(uvBaseTop);//bottom left
					uv.Add(uvBaseTop + new Vector2(0.125f, 0));//bottom right

					Color color = upFaceColor;
					if (world.BlockIsShadedAt(x, y, z)) color -= shadeColor;

					color.a = 1;

					for (int i = 0; i < 4; i++)
						colors.Add(color);
				}
			}
		}
		else
		{
			if (offset1 == Vector3.up)
			{
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
			}
			else if (offset1 == new Vector3(1, 0, -1) || offset1 == new Vector3(-1, 0, -1))
			{
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left
			}
			else
			{
				uv.Add(uvBase);//bottom left
				uv.Add(uvBase + new Vector2(0, 0.125f));//top left
				uv.Add(uvBase + new Vector2(0.125f, 0));//bottom right
				uv.Add(uvBase + new Vector2(0.125f, 0.125f));//top right
			}
			Color color = upFaceColor;
			if (world.BlockIsShadedAt(x, y, z)) color -= shadeColor;

			color.a = 1;

			for (int i = 0; i < 4; i++)
				colors.Add(color);
		}

		tris.Add(index + 0);
		tris.Add(index + 1);
		tris.Add(index + 2);
		tris.Add(index + 3);
		tris.Add(index + 2);
		tris.Add(index + 1);

		if(block < 29)
		{
			coll_tris.Add(coll_index + 0);
			coll_tris.Add(coll_index + 1);
			coll_tris.Add(coll_index + 2);
			coll_tris.Add(coll_index + 3);
			coll_tris.Add(coll_index + 2);
			coll_tris.Add(coll_index + 1);
		}
	}

    public bool IsAirBlock(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= size || y >= size || z >= size)
        {
            int worldX = Mathf.RoundToInt(transform.position.x) + x;
            int worldY = Mathf.RoundToInt(transform.position.y) + y;
            int worldZ = Mathf.RoundToInt(transform.position.z) + z;

            byte block = World.Instance.GetBlock(worldX, worldY, worldZ);

            // 判断是否为空气块应考虑当前的图形模式
            if (GraphicsSettingsManager.Instance.gMode == GraphicsMode.Fast)
                return block == 0 || block >= 29;
            else
                return block == 0 || block == 14 || block >= 29;
        }
        else
        {
            // 当块在当前Chunk的map数组范围内时，直接判断map中的值
            if (GraphicsSettingsManager.Instance.gMode == GraphicsMode.Fast)
                return map[x, y, z] == 0 || map[x, y, z] >= 29;
            else
                return map[x, y, z] == 0 || map[x, y, z] == 14 || map[x, y, z] >= 29;
        }
    }


    public void Regenerate()
	{
		if (!initialized) return;

		int chunkX = Mathf.RoundToInt(transform.position.x);
		int chunkY = Mathf.RoundToInt(transform.position.y);
		int chunkZ = Mathf.RoundToInt(transform.position.z);

		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				for (int z = 0; z < size; z++)
				{
					byte block = world.GetBlock(chunkX + x, chunkY + y, chunkZ + z);
					map[x, y, z] = block;
				}
			}
		}

		CreateMeshTarget();

		mesh.triangles = tris.ToArray();
		coll_mesh.triangles = coll_tris.ToArray();

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				for (int z = 0; z < size; z++)
				{
					byte block = map[x, y, z];
					if (block == 0) continue;

					DrawBlock(x, y, z, block);
				}
			}
		}
		CloseMeshTarget();
	}
	public void SetBlock(int x, int y, int z, byte block)
	{
		if (y == 0) return;
		x -= Mathf.RoundToInt(transform.position.x);
		y -= Mathf.RoundToInt(transform.position.y);
		z -= Mathf.RoundToInt(transform.position.z);

		if ((x < 0) || (y < 0) || (z < 0) || (x >= size) || (y >= size) || (z >= size)) return;

		if (map[x, y, z] != block)
		{
			map[x, y, z] = block;
			Regenerate();
		}
	} 

	bool BlockRequiresCollider(int x, int y, int z)
	{
		if ((x < 0) || (y < 0) || (z < 0) || (x >= size) || (y >= size) || (z >= size))
			return false;

		bool up = IsAirBlock(x, y + 1, z);
		bool down = IsAirBlock(x, y - 1, z);
		bool right = IsAirBlock(x + 1, y, z);
		bool left = IsAirBlock(x - 1, y, z);
		bool fwd = IsAirBlock(x, y, z + 1);
		bool back = IsAirBlock(x, y, z - 1);

		return up || down || right || left || fwd || back;
	}
}