using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIO : MonoBehaviour
{
	public float maxReachDist = 5;
	public GameObject retDel;
	public GameObject retAdd;

	World world;

	public string[] blockSounds;

	public byte[] hotbarBlocks = new byte[9];
	public float[] indicatorXPositions = new float[9];

	public Transform indicator;

	[HideInInspector] public int currentSlot = 0;

	public Sprite[] spritesByBlockID;
	public Image[] hotbarBlockSprites = new Image[9];

	public GameObject inventory;

	void Start()
	{
		world = World.Instance;
	}

	void Update()
	{
		if (world == null) return;

		if(Input.GetKeyUp(KeyCode.Escape))
		{
			Invoke("WorkAroundForUnitysStupidMouseHidingSystemLikeWhatTheHell", 0.1f);
		}

		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
		if (Physics.Raycast(ray, out hit, maxReachDist) && hit.collider.tag == "Chunk" && !inventory.activeSelf && !PauseMenu.pauseMenu.paused)
		{
			Vector3 p = hit.point - hit.normal / 2;
			Vector3 p2 = hit.point + hit.normal / 2;

			float delX = Mathf.Floor(p.x) + 0.5f;
			float delY = Mathf.Floor(p.y) + 0.5f;
			float delZ = Mathf.Floor(p.z) + 0.5f;

			float addX = Mathf.Floor(p2.x) + 0.5f;
			float addY = Mathf.Floor(p2.y) + 0.5f;
			float addZ = Mathf.Floor(p2.z) + 0.5f;

			p = new Vector3(delX, delY, delZ);
			p2 = new Vector3(addX, addY, addZ);

			int blockDelX = (int)(delX - 0.5f);
			int blockDelY = (int)(delY - 0.5f);
			int blockDelZ = (int)(delZ - 0.5f) + 1;

			int blockAddX = (int)(addX - 0.5f);
			int blockAddY = (int)(addY - 0.5f);
			int blockAddZ = (int)(addZ - 0.5f) + 1;

			bool delSwitch = false;

			retDel.SetActive(true);
			if (world.GetBlock(blockAddX, blockAddY, blockAddZ) >= 29)
			{
				retDel.transform.position = p2;
				delSwitch = true;
			}
			else
				retDel.transform.position = p;

			retAdd.transform.position = p2;

			if (Input.GetMouseButtonDown(0))
			{
				if (delSwitch)
				{
					blockDelX = blockAddX;
					blockDelY = blockAddY;
					blockDelZ = blockAddZ;
				}

				int b = world.world[blockDelX, blockDelY, blockDelZ];
				if (b == 0) b++;

				world.PlaceBlock(blockDelX, blockDelY, blockDelZ, 0);

				if (world.world[blockDelX, blockDelY, blockDelZ] == 0)
					SoundManager.PlayAudio(blockSounds[b - 1] + Random.Range(1, 5).ToString(), 0.2f, Random.Range(0.9f, 1.1f));

				Chunk chunk = hit.collider.gameObject.GetComponent<Chunk>();
				if (chunk == null) print(hit.transform.gameObject.name);

				int cX = blockDelX - (int)chunk.transform.position.x;
				int cY = blockDelY - (int)chunk.transform.position.y;
				int cZ = blockDelZ - (int)chunk.transform.position.z;

				Vector3Int cPos = new Vector3Int(Mathf.FloorToInt(blockDelX / 16f),
					Mathf.FloorToInt(blockDelY / 16f), Mathf.FloorToInt(blockDelZ / 16f));

				if (cX == 0)
				{
					if (world.ChunkIsWithinBounds(cPos.x - 1, cPos.y, cPos.z))
					{
						if (world.ChunkExistsAt(cPos.x - 1, cPos.y, cPos.z))
							world.chunks[cPos.x - 1, cPos.y, cPos.z].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x - 1, cPos.y, cPos.z);
					}
				}
				else if (cX == world.chunkSize - 1)
				{
					if (world.ChunkIsWithinBounds(cPos.x + 1, cPos.y, cPos.z))
					{
						if (world.ChunkExistsAt(cPos.x + 1, cPos.y, cPos.z))
							world.chunks[cPos.x + 1, cPos.y, cPos.z].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x + 1, cPos.y, cPos.z);
					}
				}

				if (cY == world.chunkSize - 1)
				{
					if (world.ChunkIsWithinBounds(cPos.x, cPos.y + 1, cPos.z))
					{
						if (world.ChunkExistsAt(cPos.x, cPos.y + 1, cPos.z))
							world.chunks[cPos.x, cPos.y + 1, cPos.z].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x, cPos.y + 1, cPos.z);
					}
				}

				if (cZ == 0)
				{
					if (world.ChunkIsWithinBounds(cPos.x, cPos.y, cPos.z - 1))
					{
						if (world.ChunkExistsAt(cPos.x, cPos.y, cPos.z - 1))
							world.chunks[cPos.x, cPos.y, cPos.z - 1].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x, cPos.y, cPos.z - 1);
					}
				}
				else if (cZ == world.chunkSize - 1)
				{
					if (world.ChunkIsWithinBounds(cPos.x, cPos.y, cPos.z + 1))
					{
						if (world.ChunkExistsAt(cPos.x, cPos.y, cPos.z + 1))
							world.chunks[cPos.x, cPos.y, cPos.z + 1].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x, cPos.y, cPos.z + 1);
					}
				}

				for (int y = cPos.y; y >= 0; y--)
				{
					if (world.ChunkExistsAt(cPos.x, y, cPos.z))
						world.chunks[cPos.x, y, cPos.z].Regenerate();
					else
						world.ForceLoadChunkAt(cPos.x, y, cPos.z);
				}

				chunk.Regenerate();
			}
			if (Input.GetMouseButtonDown(1))
			{
				byte newBlock = hotbarBlocks[currentSlot];

				if (!retAdd.GetComponent<AddReticule>().touchingPlayer)
				{

					world.PlaceBlock(blockAddX, blockAddY, blockAddZ, newBlock);

					SoundManager.PlayAudio(blockSounds[newBlock - 1] + Random.Range(1, 5).ToString(), 0.2f, Random.Range(0.9f, 1.1f));

					Chunk chunk = hit.collider.gameObject.GetComponent<Chunk>();

					int cX = blockDelX - (int)chunk.transform.position.x;
					int cY = blockDelY - (int)chunk.transform.position.y;
					int cZ = blockDelZ - (int)chunk.transform.position.z;

					Vector3Int cPos = new Vector3Int(Mathf.FloorToInt(blockDelX / 16f),
						Mathf.FloorToInt(blockDelY / 16f), Mathf.FloorToInt(blockDelZ / 16f));

					if (cX == 0)
					{
						if (world.ChunkIsWithinBounds(cPos.x - 1, cPos.y, cPos.z))
						{
							if (world.ChunkExistsAt(cPos.x - 1, cPos.y, cPos.z))
								world.chunks[cPos.x - 1, cPos.y, cPos.z].Regenerate();
							else
								world.ForceLoadChunkAt(cPos.x - 1, cPos.y, cPos.z);
						}
					}
					else if (cX == world.chunkSize - 1)
					{
						if (world.ChunkIsWithinBounds(cPos.x + 1, cPos.y, cPos.z))
						{
							if (world.ChunkExistsAt(cPos.x + 1, cPos.y, cPos.z))
								world.chunks[cPos.x + 1, cPos.y, cPos.z].Regenerate();
							else
								world.ForceLoadChunkAt(cPos.x + 1, cPos.y, cPos.z);
						}
					}

					if (cY == world.chunkSize - 1)
					{
						if (world.ChunkIsWithinBounds(cPos.x, cPos.y + 1, cPos.z))
						{
							if (world.ChunkExistsAt(cPos.x, cPos.y + 1, cPos.z))
								world.chunks[cPos.x, cPos.y + 1, cPos.z].Regenerate();
							else
								world.ForceLoadChunkAt(cPos.x, cPos.y + 1, cPos.z);
						}
					}

					if (cZ == 0)
					{
						if (world.ChunkIsWithinBounds(cPos.x, cPos.y, cPos.z - 1))
						{
							if (world.ChunkExistsAt(cPos.x, cPos.y, cPos.z - 1))
								world.chunks[cPos.x, cPos.y, cPos.z - 1].Regenerate();
							else
								world.ForceLoadChunkAt(cPos.x, cPos.y, cPos.z - 1);
						}
					}
					else if (cZ == world.chunkSize - 1)
					{
						if (world.ChunkIsWithinBounds(cPos.x, cPos.y, cPos.z + 1))
						{
							if (world.ChunkExistsAt(cPos.x, cPos.y, cPos.z + 1))
								world.chunks[cPos.x, cPos.y, cPos.z + 1].Regenerate();
							else
								world.ForceLoadChunkAt(cPos.x, cPos.y, cPos.z + 1);
						}
					}

					for (int y = cPos.y; y >= 0; y--)
					{
						if (world.ChunkExistsAt(cPos.x, y, cPos.z))
							world.chunks[cPos.x, y, cPos.z].Regenerate();
						else
							world.ForceLoadChunkAt(cPos.x, y, cPos.z);
					}

					chunk.Regenerate();
				}
			}
			if (Input.GetMouseButtonDown(2))
			{
				int b = world.GetBlock(blockDelX, blockDelY, blockDelZ);
				if(delSwitch) b = world.GetBlock(blockAddX, blockAddY, blockAddZ);

				if (!HotbarContainsBlock((byte)b))
				{
					if (b != 0 && b != 4 && (b < 8 || b > 12))
						SetHotbarBlock(b);
				}
				else
				{
					currentSlot = GetHotbarSlotWith(b);
				}
			}
		}
		else
		{
			retDel.SetActive(false);
		}

		if (Input.GetAxis("Mouse ScrollWheel") > 0)
		{
			if (currentSlot == 8) currentSlot = 0;
			else currentSlot++;
		}
		if (Input.GetAxis("Mouse ScrollWheel") < 0)
		{
			if (currentSlot == 0) currentSlot = 8;
			else currentSlot--;
		}

		for(int i = 0; i < hotbarBlockSprites.Length; i++)
			hotbarBlockSprites[i].sprite = spritesByBlockID[hotbarBlocks[i] - 1];

		indicator.localPosition = new Vector3
			(indicatorXPositions[currentSlot],
			indicator.localPosition.y,
			indicator.localPosition.z);

		if(Input.GetKeyDown(KeyCode.E))
		{
			if (inventory.activeSelf)
			{
				GetComponent<PlayerController>().controlsEnabled = false;
				inventory.SetActive(false);
			}
			else inventory.SetActive(true);
		}
	}

	public void SetHotbarBlock(int block)
	{
		if (block == 0) block++;
		if (HotbarContainsBlock((byte)block)) currentSlot = GetHotbarSlotWith(block);
		else hotbarBlocks[currentSlot] = (byte)block;
	}

	public void CloseInventory()
	{
		inventory.SetActive(false);
		GetComponent<PlayerController>().controlsEnabled = true;
	}

	bool HotbarContainsBlock(byte b)
	{
		for(int i = 0; i < hotbarBlocks.Length; i++)
			if (hotbarBlocks[i] == b) return true;

		return false;
	}

	int GetHotbarSlotWith(int b)
	{
		for (int i = 0; i < hotbarBlocks.Length; i++)
			if (hotbarBlocks[i] == b) return i;

		return -1;
	}

	void WorkAroundForUnitysStupidMouseHidingSystemLikeWhatTheHell()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}