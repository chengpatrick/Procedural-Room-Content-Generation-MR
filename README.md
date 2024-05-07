
## Summary
The purpose of the project is to create a method to do procedural content decor in a mixed reality room setting, for a room that is scanned by Meta Quest 3. This was originally a tool for one of my projects, but in generation it could become a foundation for other that would also want to do PCG in mixed reality.

The idea is to first iterate thru all the walls inside of the room. Using the corners of the walls, we find the bounds of the room. With the bounds, we can use the wave functions collapse(WFC) algorithm to generate objects in the proper location within the room.

## How to use
1. Clone this repo
2. Open Scene "RoomPGTest" under Assets -> PRG MR -> Scene
3. Look at the inside of the "room"
![[Pasted image 20240506020028.png]]
4. Choose the WFC game object, click on Start WFC to generate objects in the room
![[Pasted image 20240506020121.png]]
![[Pasted image 20240506020146.png]]
5. To import into your own project, go to files Assets -> PRG MR -> Prefabs -> Generation Prefabs, and move the two objects into your own scene. This could be used in both the editor and AR/VR/MR with a scanned room via Meta Quest.

## Setup
This is the structure for the room, it contains multiple instantiators for the floor, wall, ceiling, etc, which holds the mesh, collider and components for the parts of the room. These are all Photon Instantiator so it can be spawn thru photon engine network and applied in colocation.
![[Pasted image 20240318022303.png]]
The is the structure for a general instantiator, the mesh and collider is in the Quad component.
![[Pasted image 20240318022035.png]]

## Room Bound
We first find all wall objects, and use this function to find all vertices of the corners of the wall. Since a wall is just a plane, we can just get the four corners. 
```
    private Vector3[] GetBoxColliderVertices(BoxCollider boxCollider)
    {
        // Calculate the vertices of the box collider
        // Since walls are quads, we only use front 4 points
        Vector3 center = boxCollider.center;
        Vector3 size = boxCollider.size;
        Vector3 halfSize = size / 2f;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z))); // Front bottom left
        vertices[1] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z)));  // Front bottom right
        vertices[2] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z)));  // Front top left
        vertices[3] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)));   // Front top right

        return vertices;
    }
```

Now we spawn spheres at the location of the vertices to visualize where the bounds of the room is. We use a HashSet to store the locations of where spheres has been spawned so spheres at corners at adjacent walls don't overlap.
```
		spherePos = new HashSet<Vector3>();
        int count = 0;
        foreach (GameObject go in targets)
        {
            string name = "Wall";
            Vector3[] vertices = GetBoxColliderVertices(go.transform.GetChild(1).GetChild(0).GetComponent<BoxCollider>());
            for (int i = 0; i < vertices.Length; i++)
            {
                if(SpawnSphere(vertices[i], count, name))
                    count++;
            }
        }        
```

```
    private bool SpawnSphere(Vector3 position, int i, string name)
    {
        // Instantiate a sphere at the given position
        // Check set if position has sphere overlap
        if(!spherePos.Contains(position))
        {
            GameObject go = Instantiate(ball, position, Quaternion.identity);
            go.name += name + " num " + i;

            spherePos.Add(position);

            return true;
        }
        return false;
    }
```

As we can see from the image, spheres spawn at the corners of the walls.
![[Pasted image 20240318025637.png]]
![[Pasted image 20240318025742.png]]

Now with the locations of the spheres, we can find the bounding of the room to limit the area to perform the WFC. We iterate through the parent room object to find all the "wall" objects, find the bounds of the walls and store them within the hashset.

As we iterate through all the vertices on each wall, we want to keep track of the edge corners of the room, corresponding to minX, maxX, minZ, maxZ. As these bounding values will be used for the WFC in the upcoming steps.

## Wave Function Collapse
Though it's called WFC, it's a very simplified version WFC. I state there are only 5 states that could be possible for the grid/room setup.
      0 - Empty cell
	  1 - Small object
      2 - Medium object
      3 - Large object
      4 - Corner object of room
  ```
      [Flags]
    private enum WfcMap
    {
        None = 1 << 0, // 1
        Small = 1 << 1, // 2
        Medium = 1 << 2, // 4
        Large = 1 << 3, // 8
        Corner = 1 << 4 // 16
    };
```
The rules above can be redefined based on the needs. I have also made a rule of the number of certain objects that could exist within the generation, which is the total number of available cells times the fill percentage that is defined by the user. In my case, I will fill 30% of the cells up with objects. For example, in a 10x10 grid if all grids are available, 30 grids will be populated with objects.

All cells initially can turn out to be any object, but we can set up neighboring rules so some objects can't be next to each other. Let's say empty cell can be next to any object, but objects can't be next to another object. 
      Neighbor rules:
      0 : 1, 2, 3
      1 : 0
      2 : 0
      3 : 0
  
If we decide a cell at (1, 1) is small object, then cell adjacent to it can't have any objects. 
```
    // Update cell based on prev cell state rules
    private void UpdateCell(WfcMap prev, int i, int j)
    {
        if (prev is WfcMap.None)
        {
            map[i, j] &= ~WfcMap.None;
        }
        else if (prev is WfcMap.Small)
        {
            map[i, j] = WfcMap.None;
        }
        else if (prev is WfcMap.Medium)
        {
            map[i, j] = WfcMap.None;
        }
        else if (prev is WfcMap.Large)
        {
            map[i, j] = WfcMap.None;
        }
    }
```

As in WFC, there's also the term called entropy. As cells type are decided, choices of its neighboring cells might decreased. If the cell has more states to choose from, it has a higher entropy; if it has less states to choose from, then it has a lower entropy. We can always pick a state for a cell that has lower entropy, since there are less choices to choose from. When a cell's state is chosen and decided, that's when we call the cell is collapsed. If all cells have the same entropy, which would happen like in the very beginning, we can choose a random cell to start the collapsing process. This is the main idea of WFC.
      Alg:
      1. Find random cell to start with, choose random state outside of None
      2. Find cell with lowest entropy(choices), collapse that cell, repeat
      3. If all cell entropy is equal, choose random cell to execute
      4. Collapse all cells
Since our rules are very simple, the WFC is also very straight forward and simple.
After using WFC to generate a 2D grid that has the position of the objects, map it to the room bound using the minX, maxX, minZ, maxZ positions. Below is a sample of how I mapped the grid to the room bound.
```
 private void SpawnMapInRoom()
 {
     int i = 0, j = 0;
     GameObject instantiateParent = new GameObject("WFCParent");

     float xDiff = (minX.x - minZ.x) / m;
     float zDiff = (maxX.z - minZ.z) / n;

     for (float a = minZ.x; a < maxX.x - 0.01f; a += (maxX.x - minZ.x) / n)
     {
         j = 0;

         for (float b = minZ.z; b < minX.z - 0.01f; b += (minX.z - minZ.z) / m)
         {
             float xPos = a;
             float zPos = b;

             if (i > 0)
                 zPos += zDiff * i;
             if (j > 0)
                 xPos += xDiff * j;

             Vector3 pos = new Vector3(xPos, 0.2f, zPos);

             GameObject instantiateObj;
             switch (map[i, j])
             {
                 case WfcMap.None:
                     instantiateObj = Instantiate(NonePrefab, pos, Quaternion.identity);
                     instantiateObj.transform.parent = instantiateParent.transform;
                     break;
                 case WfcMap.Small:
                     instantiateObj = Instantiate(SmallPrefab, pos, Quaternion.identity);
                     instantiateObj.transform.parent = instantiateParent.transform;
                     break;
                 case WfcMap.Medium:
                     instantiateObj = Instantiate(MediumPrefab, pos, Quaternion.identity);
                     instantiateObj.transform.parent = instantiateParent.transform;
                     break;
                 case WfcMap.Large:
                     instantiateObj = Instantiate(LargePrefab, pos, Quaternion.identity);
                     instantiateObj.transform.parent = instantiateParent.transform;
                     break;
                 case WfcMap.Corner:
                     instantiateObj = Instantiate(CornerPrefab, pos, Quaternion.identity);
                     instantiateObj.transform.parent = instantiateParent.transform;
                     break;
                 default:
                     break;
             }
             j++;
         }
         i++;
     }
 }
```

This works ideally in a empty square or rectangular room. 
As there are not many PCG related to MR on the web at the moment. I hope this repo and the scripts provided can be helpful.

## Other
The original template of this project is from oculus-samples/Unity-Discover. Everything related to the Procedural Room Generation MR project is under the fold PRG MR.
This is not industry standard level of PCG, but more of a experimental tool. Feel free to play around with it, and reach out if there are any other questions.