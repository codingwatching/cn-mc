using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimplexNoise;
using System.Runtime.Serialization.Formatters.Binary;
using Cysharp.Threading.Tasks;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }          // 单例模式，全局访问点

    public int chunkSize = 16;                                  // 每个区块的大小
    public byte[,,] world;                                      // 存储世界所有方块的数组
    public Chunk[,,] chunks;                                    // 存储所有区块的数组

    [SerializeField] float viewDistance = 6;                    // 视距，决定玩家能看到多远的区块
    [SerializeField] int worldWidth = 128;                      // 世界的宽度，以区块数计
    [SerializeField] int worldHeight = 32;                      // 世界的高度，以区块数计
    [SerializeField] Chunk chunk;                               // 区块的预制体
    [SerializeField] GameObject particles;                      // 生成粒子效果的游戏对象
    [SerializeField] Texture[] particleTextures;                // 粒子效果使用的纹理数组
    [SerializeField] TMPro.TextMeshPro loadingWorldInfo;        // 显示加载世界信息的文本
    [SerializeField] TMPro.TextMeshPro loadingWorldInfoShadow;  // 显示加载世界信息的文本阴影
    [SerializeField] GameObject loadingScreen;                  // 加载屏幕的游戏对象
    [SerializeField] AudioSource music;                         // 背景音乐

    [HideInInspector] public WorldData currentWorldData;        // 当前世界的数据
    [HideInInspector] public bool worldInitialized = false;     // 世界是否已初始化的标志
    readonly List<Vector3Int> chunkSpawnCues = new();           // 需要生成的区块的队列
    bool finishedGeneratingChunks = false;                      // 是否完成了区块的生成
    Vector3Int lastPlayerChunkPos = Vector3Int.up * 1000;       // 上一次玩家所在的区块位置
    Vector3Int playerChunkPos;                                  // 玩家当前所在的区块位置
    Vector3Int playerPos;                                       // 玩家的位置
    bool worldIsFromSaveFile = false;                           // 标志位，表示世界是否来自保存文件
    float curNoise = 0;                                         // 当前噪声值，用于生成世界时的计算
    private int seed;                                           // 世界生成的种子值
    int counter = 0;                                            // 用于各种计数

    /// <summary>
    /// 世界的宽度，以方块数计
    /// </summary>
    public int WorldBlockWidth => worldWidth * chunkSize;       

    /// <summary>
    /// 世界的高度，以方块数计
    /// </summary>
    public int WorldBlockHeight => worldHeight * chunkSize;

    private void Awake()
    {
        Instance = this;
        world = new byte[WorldBlockWidth, WorldBlockHeight, WorldBlockWidth];
        chunks = new Chunk[worldWidth, worldHeight, worldWidth];
    }

    async void Start()
    {
        Vector3 playerPosition = PlayerController.Instance.transform.position;
        int playerChunkX = Mathf.RoundToInt(playerPosition.x / chunkSize);
        int playerChunkY = Mathf.RoundToInt(playerPosition.y / chunkSize);
        int playerChunkZ = Mathf.RoundToInt(playerPosition.z / chunkSize);
        playerChunkPos = new Vector3Int(playerChunkX, playerChunkY, playerChunkZ);
        int playerX = Mathf.RoundToInt(playerPosition.x);
        int playerY = Mathf.RoundToInt(playerPosition.y);
        int playerZ = Mathf.RoundToInt(playerPosition.z);
        playerPos = new Vector3Int(playerX, playerY, playerZ);

        LoadWorld();

        if (!worldIsFromSaveFile || world == null)
        {
            seed = Random.Range(0, 100000);
        }

        if (world == null)
        {
            worldIsFromSaveFile = false;
            world = new byte[WorldBlockWidth, WorldBlockHeight, WorldBlockWidth];

            PlayerController.Instance.PlayerIO.hotbarBlocks = new byte[] { 5, 13, 14, 15, 5, 23, 24, 1, 2 };
            PlayerController.Instance.PlayerIO.currentSlot = 0;
        }

        Random.InitState(seed);

        if (worldIsFromSaveFile)
        {
            loadingWorldInfo.text = "Loading World";
            loadingWorldInfoShadow.text = "Loading World";

            await UniTask.WaitForSeconds(1f);
            GenerateStartChunks();
        }
        else
        {

            await UniTask.WaitForSeconds(2f);
            GenerateWorld();
            await UniTask.WaitForSeconds(0.05f);
            SpawnOres();
            await UniTask.WaitForSeconds(0.25f);
            GenerateCaves();
            await UniTask.WaitForSeconds(0.25f);
            Plant();
            await UniTask.WaitForSeconds(1f);
            GenerateStartChunks();
        }

        await UniTask.WaitForSeconds(1.75f);
        FloorPlayer();

        worldInitialized = true;
    }

    void Update()
    {
        Transform player = PlayerController.Instance.transform;
        int playerChunkX = Mathf.RoundToInt((player.position.x) / chunkSize);
        int playerChunkY = Mathf.RoundToInt((player.position.y) / chunkSize);
        int playerChunkZ = Mathf.RoundToInt((player.position.z) / chunkSize);

        playerChunkPos = new Vector3Int(playerChunkX, playerChunkY, playerChunkZ);

        int playerX = Mathf.RoundToInt(player.position.x);
        int playerY = Mathf.RoundToInt(player.position.y);
        int playerZ = Mathf.RoundToInt(player.position.z);

        playerPos = new Vector3Int(playerX, playerY, playerZ);

        if (playerChunkPos != lastPlayerChunkPos)
        {
            finishedGeneratingChunks = false;

            GenerateChunks();

            lastPlayerChunkPos = playerChunkPos;
            counter = 0;
        }
    }

    #region 生成地图

    void GenerateWorld()
    {
        loadingWorldInfo.text = "Generating World";
        loadingWorldInfoShadow.text = "Generating World";

        for (int x = 0; x < WorldBlockWidth; x++)
        {
            for (int y = 0; y < WorldBlockHeight; y++)
            {
                for (int z = 0; z < WorldBlockWidth; z++)
                {
                    float noiseScale = 30 + (Mathf.PerlinNoise((x + seed) / 75, (z + seed) / 75) * 30);
                    float sandBiome = Mathf.PerlinNoise((x - 512 + seed) / 100f, (z - 512 + seed) / 100f) * 50f;

                    float noise = Noise.Generate((x + seed) / noiseScale, y / noiseScale, (z + seed) / noiseScale);

                    float dividendSub = ((sandBiome - 20) / 3f);

                    noise += (WorldBlockHeight - y - 25) / (10f - dividendSub);
                    curNoise = noise;

                    if (sandBiome > 30)
                    {
                        if (noise > 0.1f)
                        {
                            world[x, y, z] = 6;
                        }
                        if (noise > 0.8f) world[x, y, z] = 3;
                    }
                    else
                    {
                        if (noise > 0.1f)
                        {
                            world[x, y, z] = 1;
                        }
                        if (noise > 0.4f) world[x, y, z] = 3;
                    }

                    // BEDROCK
                    if (y == 0) world[x, y, z] = 4;
                    if (x == 0 || x == WorldBlockWidth - 1 || z == 0 || z == WorldBlockWidth - 1)
                    {
                        if (noise > 0.4f)
                            world[x, y, z] = 4;
                    }
                }
            }
        }
    }

    void SpawnOres()
    {
        loadingWorldInfo.text = "Spawning Ores";
        loadingWorldInfoShadow.text = "Spawning Ores";

        for (int x = 0; x < WorldBlockWidth; x++)
        {
            for (int y = 0; y < WorldBlockHeight; y++)
            {
                for (int z = 0; z < WorldBlockWidth; z++)
                {
                    float coal = Noise.Generate((x + seed + 512) / 10f, (y + seed + 512) / 10f, (z + seed + 512) / 10f);
                    float iron = Noise.Generate((x + seed + 256) / 14f, (y + seed + 256) / 18f, (z + seed + 256) / 14f);
                    float redstone = Noise.Generate((x + seed + 1028) / 17f, (y + seed + 1028) / 17f, (z + seed + 1028) / 17f);
                    float gold = Noise.Generate((x + seed + 2048) / 20f, (y + seed + 2048) / 20f, (z + seed + 2048) / 20f);
                    float diamond = Noise.Generate((x + seed + 4096) / 20f, (y + seed + 4096) / 20f, (z + seed + 4096) / 20f);

                    float groundDirt = Noise.Generate((x + seed + 1000) / 30f, (y + seed + 1000) / 30f, (z + seed + 1000) / 30f);
                    float groundGravel = Noise.Generate((x + seed + 2000) / 32f, (y + seed + 2000) / 32f, (z + seed + 2000) / 32f);

                    // ORES
                    if (coal >= 0.875f && curNoise > 0.4f) world[x, y, z] = 8;
                    if (iron >= 0.92f && curNoise > 0.4f && y < 64) world[x, y, z] = 9;
                    if (redstone >= 0.94f && curNoise > 0.4f && y < 32) world[x, y, z] = 10;
                    if (gold >= 0.94f && curNoise > 0.4f && y < 52) world[x, y, z] = 11;
                    if (diamond >= 0.955f && curNoise > 0.4f && y < 45) world[x, y, z] = 12;

                    // UNDERGROUND DIRT
                    if (groundDirt >= 0.9f && curNoise > 0.4f) world[x, y, z] = 1;

                    // UNDERGROUND GRAVEL
                    if (groundGravel >= 0.9f && curNoise > 0.4f) world[x, y, z] = 7;
                }
            }
        }
    }

    void GenerateCaves()
    {
        loadingWorldInfo.text = "Digging";
        loadingWorldInfoShadow.text = "Digging";

        for (int x = 0; x < WorldBlockWidth; x++)
        {
            for (int y = 0; y < WorldBlockHeight; y++)
            {
                for (int z = 0; z < WorldBlockWidth; z++)
                {
                    float caves = Noise.Generate((x + seed) / 40f, (y + seed) / 40f, (z + seed) / 40f);

                    // CAVES
                    if (caves >= 0.8f && world[x, y, z] != 4) world[x, y, z] = 0;
                }
            }
        }

    }

    void Plant()
    {
        loadingWorldInfo.text = "Planting";
        loadingWorldInfoShadow.text = "Planting";

        int treeCount = 0;
        for (int x = 0; x < WorldBlockWidth; x++)
        {
            for (int z = 0; z < WorldBlockWidth; z++)
            {
                bool treeHere = Random.Range(0, 1000) < 5;

                if (treeHere)
                {
                    treeCount++;

                    int groundLevel = 0;

                    bool startUp = Random.Range(0, 2) == 0;
                    if (startUp)
                    {
                        for (int y = WorldBlockHeight; y <= 0; y--)
                        {
                            if (world[x, y, z] == 1 && GetBlock(x, y + 1, z) == 0)
                            {
                                groundLevel = y;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < WorldBlockHeight; y++)
                        {
                            if (world[x, y, z] == 1 && GetBlock(x, y + 1, z) == 0)
                            {
                                groundLevel = y;
                                break;
                            }
                        }
                    }

                    if (groundLevel != 0)
                    {
                        int treeHeight = Random.Range(4, 8);

                        for (int y = groundLevel + 1; y <= groundLevel + treeHeight; y++)
                            TrySetBlock(x, y, z, 13);

                        for (int x1 = x - 2; x1 <= x + 2; x1++)
                        {
                            for (int y1 = groundLevel + treeHeight - 2; y1 <= groundLevel + treeHeight + 2; y1++)
                            {
                                for (int z1 = z - 2; z1 <= z + 2; z1++)
                                {
                                    if (GetBlock(x1, y1, z1) == 13) continue;

                                    if (y1 <= groundLevel + treeHeight)
                                    {
                                        TrySetBlock(x1, y1, z1, 14);
                                    }
                                    else
                                    {
                                        if (Mathf.Abs(x1 - x) == 1 && Mathf.Abs(z1 - z) == 0 ||
                                            Mathf.Abs(x1 - x) == 0 && Mathf.Abs(z1 - z) == 1 ||
                                            (x1 - x == 0 && z1 - z == 0))
                                        {
                                            TrySetBlock(x1, y1, z1, 14);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int x = 0; x < WorldBlockWidth; x++)
        {
            for (int y = 0; y < WorldBlockHeight; y++)
            {
                for (int z = 0; z < WorldBlockWidth; z++)
                {
                    if (world[x, y, z] == 1 && GetBlock(x, y + 1, z) == 0 && y > 18)
                    {
                        float gNoise = Mathf.PerlinNoise((x + seed) / 30f, (z + seed) / 30f) * 200;
                        bool g = Random.Range(0, 30 + gNoise) < 2;
                        bool r = Random.Range(0, 600 + gNoise) < 2;
                        bool d = Random.Range(0, 600 + gNoise) < 2;

                        world[x, y, z] = 2;

                        if (g) TrySetBlock(x, y + 1, z, 29);
                        if (r) TrySetBlock(x, y + 1, z, 30);
                        if (d) TrySetBlock(x, y + 1, z, 31);
                    }
                }
            }
        }
    }

    void GenerateStartChunks()
    {
        loadingWorldInfo.text = "Building Terrain";
        loadingWorldInfoShadow.text = "Building Terrain";
        chunkSpawnCues.Clear();

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < worldWidth; z++)
                {
                    var chunkPos = new Vector3Int(x, y, z);

                    if (Vector3Int.Distance(playerChunkPos, chunkPos) <= (viewDistance / 2) &&
                        !InvisibleChunk(x, y, z) && !chunks[x, y, z])
                        chunkSpawnCues.Add(chunkPos);
                }
            }
        }

        for (int y = worldHeight - 1; y > 0; y--)
        {
            int x = playerChunkPos.x;
            int z = playerChunkPos.z;

            var chunkPos = new Vector3Int(x, y, z);

            if (!InvisibleChunk(x, y, z) && !chunks[x, y, z]) chunkSpawnCues.Add(chunkPos);
        }

        chunkSpawnCues.Sort(
            delegate (Vector3Int a, Vector3Int b)
            {
                return Vector3Int.Distance(playerPos, a * chunkSize).CompareTo(Vector3Int.Distance(playerPos, b * chunkSize));
            }
        );

        for (int i = 0; i < chunkSpawnCues.Count; i++)
        {
            int x = chunkSpawnCues[i].x;
            int y = chunkSpawnCues[i].y;
            int z = chunkSpawnCues[i].z;

            if (chunks[x, y, z]) continue;

            Vector3 spawnPos = new Vector3(x, y, z) * 16;
            Chunk newChunk = Instantiate(chunk, spawnPos, Quaternion.identity, this.transform) as Chunk;

            // Use GraphicsSettingsManager to get the current material based on the graphics mode
            Material currentChunkMaterial = GraphicsSettingsManager.Instance.GetChunkMaterial();
            newChunk.GetComponent<Renderer>().material = currentChunkMaterial;

            chunks[x, y, z] = newChunk;
        }
    }

    void FloorPlayer()
    {
        Transform player = PlayerController.Instance.transform;
        var ray = new Ray(player.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var motion = new Vector3(0, -Vector3.Distance(player.position, hit.point) + 0.5f, 0);
            player.GetComponent<CharacterController>().Move(motion);
        }
        player.GetComponent<PlayerController>().enabled = true;
        loadingScreen.SetActive(false);

        if (PauseMenu.pauseMenu.music) music.Play();
    }

    #endregion

    void GenerateChunksAroundPlayer()
    {
        chunkSpawnCues.Clear();

        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < worldWidth; z++)
                {
                    var chunkPos = new Vector3Int(x, y, z);

                    if (Vector3Int.Distance(playerChunkPos, chunkPos) <= (viewDistance / 2) && !InvisibleChunk(x, y, z) && !chunks[x, y, z])
                        chunkSpawnCues.Add(chunkPos);
                }
            }
        }

        for (int y = worldHeight - 1; y > 0; y--)
        {
            int x = playerChunkPos.x;
            int z = playerChunkPos.z;

            var chunkPos = new Vector3Int(x, y, z);

            if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
                continue;

            if (!InvisibleChunk(x, y, z) && !chunks[x, y, z]) chunkSpawnCues.Add(chunkPos);
        }

        chunkSpawnCues.Sort
        (
            delegate (Vector3Int a, Vector3Int b)
            {
                return Vector3Int.Distance(playerPos, a * chunkSize).CompareTo(Vector3Int.Distance(playerPos, b * chunkSize));
            }
        );

        for (int i = 0; i < chunkSpawnCues.Count; i++)
        {
            int x = chunkSpawnCues[i].x;
            int y = chunkSpawnCues[i].y;
            int z = chunkSpawnCues[i].z;

            Vector3 spawnPos = new Vector3(x, y, z) * 16;
            Chunk newChunk = Instantiate(chunk, spawnPos, Quaternion.identity, this.transform) as Chunk;

            // 使用 GraphicsSettingsManager 获取当前图形模式对应的材质
            Material currentMaterial = GraphicsSettingsManager.Instance.GetChunkMaterial();
            newChunk.GetComponent<Renderer>().material = currentMaterial;

            chunks[x, y, z] = newChunk;
        }
    }

    #region 更新Chunk

    async void GenerateChunks()
    {
        chunkSpawnCues.Clear();

        for (int x = playerChunkPos.x - (int)viewDistance; x <= playerChunkPos.x + viewDistance; x++)
        {
            for (int y = playerChunkPos.y - (int)(viewDistance / 2); y <= playerChunkPos.y + (viewDistance / 2); y++)
            {
                for (int z = playerChunkPos.z - (int)viewDistance; z <= playerChunkPos.z + viewDistance; z++)
                {
                    if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
                    {
                        continue;
                    }

                    var chunkPos = new Vector3Int(x, y, z);

                    if (Vector3Int.Distance(playerChunkPos, chunkPos) <= viewDistance && !InvisibleChunk(x, y, z) && !chunks[x, y, z])
                    {
                        chunkSpawnCues.Add(chunkPos);
                    }

                    if (!InvisibleChunk(x, y, z) && Random.Range(0, 10) < 5)
                    {
                        await UniTask.Yield();
                    }
                }
            }
        }

        chunkSpawnCues.Sort
        (
            delegate (Vector3Int a, Vector3Int b)
            {
                return Vector3Int.Distance(playerPos, a * chunkSize).CompareTo(Vector3Int.Distance(playerPos, b * chunkSize));
            }
        );

        counter = 0;
        SpawnNextChunk();
    }

    async void SpawnNextChunk()
    {
        if (counter >= chunkSpawnCues.Count) counter = 0;
        if (chunkSpawnCues.Count == 0) return;

        int x = chunkSpawnCues[counter].x;
        int y = chunkSpawnCues[counter].y;
        int z = chunkSpawnCues[counter].z;

        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
        {
            if (counter >= chunkSpawnCues.Count - 1)
            {
                finishedGeneratingChunks = true;
                counter = 0;
            }
            else
            {
                counter++;
                SpawnNextChunk();
            }
            return;
        }

        if (!chunks[x, y, z])
        {
            Vector3 spawnPos = new Vector3(x, y, z) * 16;
            Chunk newChunk = Instantiate(chunk, spawnPos, Quaternion.identity, this.transform) as Chunk;

            // 使用GraphicsSettingsManager来获取当前图形模式下的材质
            newChunk.GetComponent<Renderer>().material = GraphicsSettingsManager.Instance.GetChunkMaterial();

            chunks[x, y, z] = newChunk;
        }
        if (counter >= chunkSpawnCues.Count - 1)
        {
            finishedGeneratingChunks = true;
            counter = 0;
        }
        else
        {
            await UniTask.WaitForSeconds(0.05f);
            SpawnNextChunk();
            counter++;
        }
    }

    public void ForceLoadChunkAt(int x, int y, int z)
    {
        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
            return;

        if (chunks[x, y, z]) return;
        if (InvisibleChunk(x, y, z)) return;

        Vector3 spawnPos = new Vector3(x, y, z) * 16;
        Chunk newChunk = Instantiate(chunk, spawnPos, Quaternion.identity, this.transform) as Chunk;

        // 使用GraphicsSettingsManager来获取当前图形模式下的材质
        newChunk.GetComponent<Renderer>().material = GraphicsSettingsManager.Instance.GetChunkMaterial();

        chunks[x, y, z] = newChunk;
    }

    #endregion

    public byte GetBlockUnderPlayer()
    {
        int x = playerPos.x;
        int y = playerPos.y;
        int z = playerPos.z;

        return GetBlock(x, y - 2, z);
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= WorldBlockWidth || z >= WorldBlockWidth) return 1;
        if (y >= WorldBlockHeight) return 0;

        if (world == null) return 0;

        return world[x, y, z];
    }

    public void SpawnLandParticles()
    {
        if (GetBlockUnderPlayer() == 0) return;

        Transform player = PlayerController.Instance.transform;
        float x = player.position.x;
        float y = player.position.y;
        float z = player.position.z;

        GameObject p = Instantiate(particles, new Vector3(x, y - 1, z), particles.transform.rotation) as GameObject;
        p.GetComponent<Renderer>().material.mainTexture = particleTextures[GetBlockUnderPlayer() - 1];
    }

    public bool BlockIsObscured(int x, int y, int z)
    {
        bool up = GetBlock(x, y + 1, z) != 0;
        bool down = GetBlock(x, y - 1, z) != 0;
        bool right = GetBlock(x + 1, y, z) != 0;
        bool left = GetBlock(x - 1, y, z) != 0;
        bool fwd = GetBlock(x, y, z + 1) != 0;
        bool back = GetBlock(x, y, z - 1) != 0;

        return up && down && right && left && fwd && back;
    }

    public bool InvisibleChunk(int xPos, int yPos, int zPos)
    {
        bool invisible = true;

        for (int x = xPos * chunkSize; x < xPos * chunkSize + chunkSize; x++)
        {
            for (int y = yPos * chunkSize; y < yPos * chunkSize + chunkSize; y++)
            {
                for (int z = zPos * chunkSize; z < zPos * chunkSize + chunkSize; z++)
                {
                    if (GetBlock(x, y, z) != 0)
                    {
                        if (!BlockIsObscured(x, y, z))
                        {
                            invisible = false;
                        }
                    }
                    if (!invisible) break;
                    else invisible = true;
                }
                if (!invisible) break;
            }
            if (!invisible) break;
        }

        return invisible;
    }

    public bool ChunkIsObscured(int x, int y, int z)
    {
        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
            return false;

        bool up = ChunkExistsAt(x, y + 1, z);
        bool down = ChunkExistsAt(x, y - 1, z);
        bool right = ChunkExistsAt(x + 1, y, z);
        bool left = ChunkExistsAt(x - 1, y, z);
        bool fwd = ChunkExistsAt(x, y, z + 1);
        bool back = ChunkExistsAt(x, y, z - 1);

        return up && down && right && left && fwd && back && chunks[x, y, z];
    }

    public bool ChunkExistsAt(int x, int y, int z)
    {
        if (x < 0 || x >= worldWidth || y < 0 || z < 0 || z >= worldWidth)
            return false;

        return chunks[x, y, z] != null;
    }

    public bool ChunkIsWithinBounds(int x, int y, int z)
    {
        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight || z < 0 || z >= worldWidth)
            return false;
        return true;
    }

    void TrySetBlock(int x, int y, int z, byte block)
    {
        if (x < 0 || x >= WorldBlockWidth || y < 0 || y >= WorldBlockHeight || z < 0 || z >= WorldBlockWidth)
        {
            return;
        }
        world[x, y, z] = block;
    }

    public void PlaceBlock(int x, int y, int z, byte block)
    {
        if (x < 0 || x >= WorldBlockWidth || y < 0 || y >= WorldBlockHeight || z < 0 || z >= WorldBlockWidth)
            return;

        if (block == 0 && world[x, y, z] != 0 && world[x, y, z] != 4 && world[x, y, z] < 30)
        {
            GameObject p = Instantiate(particles, new Vector3(x + 0.5f, y + 0.3f, z - 0.5f), particles.transform.rotation) as GameObject;
            p.GetComponent<Renderer>().material.mainTexture = particleTextures[(int)world[x, y, z] - 1];
        }

        if (world[x, y, z] == 4) return;

        world[x, y, z] = block;
        if (block == 0 && y <= WorldBlockHeight - 1 && GetBlock(x, y + 1, z) >= 29) world[x, y + 1, z] = 0;
    }

    public bool BlockIsShadedAt(int x, int y, int z)
    {
        if (x < 0 || x >= WorldBlockWidth || y < 0 || y >= WorldBlockHeight || z < 0 || z >= WorldBlockWidth)
            return false;

        for (int y1 = y; y1 < WorldBlockHeight; y1++)
        {
            if (y1 == y) continue;

            if (GetBlock(x, y1, z) != 0) return true;
        }
        return false;
    }

    public void ChangeGraphicsMode()
    {
        UpdateGraphicsMode();
    }

    async void UpdateGraphicsMode()
    {
        // 更改图形模式
        GraphicsSettingsManager.Instance.ChangeGraphicsMode();

        // 重新加载所有区块以应用新的图形设置
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < worldWidth; z++)
                {
                    if (chunks[x, y, z])
                    {
                        Destroy(chunks[x, y, z].gameObject);
                    }
                    else
                    {
                        continue;
                    }

                    Vector3 spawnPos = new Vector3(x, y, z) * 16;
                    Chunk newChunk = Instantiate(chunk, spawnPos, Quaternion.identity, transform);

                    // 使用GraphicsSettingsManager来获取当前图形模式下的材质
                    newChunk.GetComponent<Renderer>().material = GraphicsSettingsManager.Instance.GetChunkMaterial();

                    chunks[x, y, z] = newChunk;

                    if (Random.Range(0, 100) < 5)
                    {
                        await UniTask.Yield();
                    }
                }
            }
        }

        // 如果有需要，可以在这里添加对其他图形设置更改后的特定逻辑处理
    }

    public void SaveWorld()
    {
        PlayerIO io = FindObjectOfType<PlayerIO>();

        currentWorldData.music = PauseMenu.pauseMenu.music;
        currentWorldData.hotbar = io.hotbarBlocks;
        currentWorldData.curHotbarSlot = io.currentSlot;
        // 从GraphicsSettingsManager获取当前图形模式而不是直接从World类
        currentWorldData.gMode = GraphicsSettingsManager.Instance.gMode;
        currentWorldData.invertMouse = PauseMenu.pauseMenu.invertMouse;
        currentWorldData.map = world;

        var stream = new FileStream("general.data", FileMode.Create);
        var formatter = new BinaryFormatter();

        try
        {
            formatter.Serialize(stream, currentWorldData);
        }
        catch (System.Runtime.Serialization.SerializationException e)
        {
            Debug.Log("Failed to serialize world data. Reason: " + e.Message);
            return;
        }
        finally
        {
            stream.Close();
        }
    }

    public void LoadWorld()
    {
        if (!File.Exists("general.data"))
            return;

        var stream = new FileStream("general.data", FileMode.Open);

        try
        {
            var formatter = new BinaryFormatter();
            currentWorldData = formatter.Deserialize(stream) as WorldData;
        }
        catch (System.Runtime.Serialization.SerializationException e)
        {
            Debug.Log("Failed to deserialize. Reason: " + e.Message);
            return;
        }
        finally
        {
            stream.Close();
        }

        PlayerIO io = PlayerController.Instance.PlayerIO;
        io.hotbarBlocks = currentWorldData.hotbar;
        io.currentSlot = currentWorldData.curHotbarSlot;
        GraphicsSettingsManager.Instance.gMode = currentWorldData.gMode;
        GraphicsSettingsManager.Instance.ApplyGraphicsSettings();
        PauseMenu.pauseMenu.music = currentWorldData.music;
        PauseMenu.pauseMenu.invertMouse = currentWorldData.invertMouse;
        world = currentWorldData.map;

        worldIsFromSaveFile = true;
    }

    public async void GenerateNewWorld()
    {
        await UniTask.WaitForSeconds(0.2f);
        NewWorldGen();
    }

    void NewWorldGen()
    {
        SaveWorld();

        currentWorldData.map = null;

        var stream = new FileStream("general.data", FileMode.Create);
        var formatter = new BinaryFormatter();

        try
        {
            formatter.Serialize(stream, currentWorldData);
        }
        catch (System.Runtime.Serialization.SerializationException e)
        {
            Debug.Log("Failed to serialize world data. Reason: " + e.Message);
            return;
        }
        finally
        {
            stream.Close();
        }

        SceneManager.LoadScene(1);
    }

    void OnApplicationQuit()
    {
        SaveWorld();
    }
}