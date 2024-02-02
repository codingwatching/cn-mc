using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsBlock : MonoBehaviour
{
	public byte blockID;

	World world;

	void Awake()
	{
		world = World.Instance;
	}

	void Update()
	{
		if(Physics.Raycast(new Ray(transform.position, Vector3.down), 0.51f))
		{
			int bX = Mathf.FloorToInt(transform.position.x);
			int bY = Mathf.FloorToInt(transform.position.y);
			int bZ = Mathf.FloorToInt(transform.position.z);

			world.PlaceBlock(bX, bY, bZ, blockID);
		}
	}
}
