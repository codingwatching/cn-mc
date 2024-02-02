[System.Serializable]
public class WorldData
{
	public byte[,,] map;
	public byte[] hotbar;
	public int curHotbarSlot;

	public float xPos;
	public float yPos;
	public float zPos;
	public float playerYRot;
	public float playerXRot;

	public bool music;
	public GraphicsMode gMode;
	public bool invertMouse;
}
