using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWakes : MonoBehaviour {

    Mesh waterMesh;
    MeshFilter waterMeshFilter;
    MeshCollider waterMeshCollider;

    // Size of the water mesh
    float waterWidth = 10f;
    // Distance between water vertices
    float gridSpacing = 0.15f;

    //Used in the iWave loop
    Vector3[][] height;
    Vector3[][] previousHeight;
    Vector3[][] verticalDerivative;
    public Vector3[][] source;
    public Vector3[][] obstruction;
    //To update the mesh we need a 1d array
    public Vector3[] unfolded_verts;
    //To be able to add ambient waves
    public Vector3[][] heightDifference;
    //Faster to calculate this once
    int arrayLength;
    float updateTimer = 0f;

    // Dampening value
    public float alpha = 0.6f;
    // kernel size
    int P = 8;
    float g = 9.81f;
    float[,] storedKernelArray;

    Vector3[][] CloneList(Vector3[][] arrayToClone)
    {
        //First clone the outer array
        Vector3[][] newArray = arrayToClone.Clone() as Vector3[][];

        //Then clone the inner arrays
        for (int i = 0; i < newArray.Length; i++)
        {
            newArray[i] = newArray[i].Clone() as Vector3[];
        }

        return newArray;
    }

    void MoveWater(float dt)
    {
        //This will update height[j][i]
        AddWaterWakes(dt);


        //Update the mesh with the new heights
        //Unfold the 2d array of verticies into a 1d array
        for (int i = 0; i < arrayLength; i++)
        {
            //Copies all the elements of the current array to the specified array
            heightDifference[i].CopyTo(unfolded_verts, i * heightDifference.Length);
        }

        //Add the new position of the water to the water mesh
        waterMesh.vertices = unfolded_verts;
        //Ensure the bounding volume is correct
        waterMesh.RecalculateBounds();
        //After modifying the vertices it is often useful to update the normals to reflect the change
        waterMesh.RecalculateNormals();
    }

    void AddWaterWakes(float dt)
    {
        //If strange gigantic waves happens, adjust alpha

        //Add sources and obstructions
        for (int j = 0; j < arrayLength; j++)
        {
            for (int i = 0; i < arrayLength; i++)
            {
                //Add height from source
                height[j][i].y += source[j][i].y;

                //Clear the source
                source[j][i].y = 0f;

                //Add obstruction
                height[j][i].y *= obstruction[j][i].y;
            }
        }


        //Convolve to update verticalDerivative
        Convolve();

        //Same for all
        float twoMinusAlphaTimesDt = 2f - alpha * dt;
        float onePlusAlphaTimesDt = 1f + alpha * dt;
        float gravityTimesDtTimesDt = g * dt * dt;

        for (int j = 0; j < arrayLength; j++)
        {
            for (int i = 0; i < arrayLength; i++)
            {
                //Faster to do this once
                float currentHeight = height[j][i].y;

                //Calculate the new height
                float newHeight = 0f;

                newHeight += currentHeight * twoMinusAlphaTimesDt;
                newHeight -= previousHeight[j][i].y;
                newHeight -= gravityTimesDtTimesDt * verticalDerivative[j][i].y;
                newHeight /= onePlusAlphaTimesDt;

                //Save the old height
                previousHeight[j][i].y = currentHeight;

                //Add the new height
                height[j][i].y = newHeight;

                //If we have ambient waves we can add them here
                //Just replace this with a call to a method where you find the current height of the ambient wave
                //At the current coordinate
                float heightAmbientWave = 0f;

                heightDifference[j][i].y = heightAmbientWave + newHeight;
            }
        }
    }

    void Convolve()
    {
        for (int j = 0; j < arrayLength; j++)
        {
            for (int i = 0; i < arrayLength; i++)
            {
                float vDeriv = 0f;

                //Will include when k, l = 0
                for (int k = -P; k <= P; k++)
                {
                    for (int l = -P; l <= P; l++)
                    {
                        //Get the precomputed values
                        //Need "+ P" because we iterate from -P and not 0, which is how they are stored in the array
                        float kernelValue = storedKernelArray[k + P, l + P];

                        //Make sure we are within the water
                        if (j + k >= 0 && j + k < arrayLength && i + l >= 0 && i + l < arrayLength)
                        {
                            vDeriv += kernelValue * height[j + k][i + l].y;
                        }
                        //Outside
                        else
                        {
                            //Right
                            if (j + k >= arrayLength && i + l >= 0 && i + l < arrayLength)
                            {
                                vDeriv += kernelValue * height[2 * arrayLength - j - k - 1][i + l].y;
                            }
                            //Top
                            else if (i + l >= arrayLength && j + k >= 0 && j + k < arrayLength)
                            {
                                vDeriv += kernelValue * height[j + k][2 * arrayLength - i - l - 1].y;
                            }
                            //Left
                            else if (j + k < 0 && i + l >= 0 && i + l < arrayLength)
                            {
                                vDeriv += kernelValue * height[-j - k][i + l].y;
                            }
                            //Bottom
                            else if (i + l < 0 && j + k >= 0 && j + k < arrayLength)
                            {
                                vDeriv += kernelValue * height[j + k][-i - l].y;
                            }
                        }
                    }
                }

                verticalDerivative[j][i].y = vDeriv;
            }
        }
    }    

    void PrecomputeKernelValues()
    {
        float G_zero = CalculateG_zero();

        //P - kernel size
        for (int k = -P; k <= P; k++)
        {
            for (int l = -P; l <= P; l++)
            {
                //Need "+ P" because we iterate from -P and not 0, which is how they are stored in the array
                storedKernelArray[k + P, l + P] = CalculateG((float)k, (float)l, G_zero);
            }
        }
    }

    private float CalculateG(float k, float l, float G_zero)
    {
        float delta_q = 0.001f;
        float sigma = 1f;
        float r = Mathf.Sqrt(k * k + l * l);

        float G = 0f;
        for (int n = 1; n <= 10000; n++)
        {
            float q_n = ((float)n * delta_q);
            float q_n_square = q_n * q_n;

            G += q_n_square * Mathf.Exp(-sigma * q_n_square) * BesselFunction(q_n * r);
        }

        G /= G_zero;

        return G;
    }

    private float CalculateG_zero()
    {
        float delta_q = 0.001f;
        float sigma = 1f;

        float G_zero = 0f;
        for (int n = 1; n <= 10000; n++)
        {
            float q_n_square = ((float)n * delta_q) * ((float)n * delta_q);

            G_zero += q_n_square * Mathf.Exp(-sigma * q_n_square);
        }

        return G_zero;
    }

    private float BesselFunction(float x)
    {
        //From http://people.math.sfu.ca/~cbm/aands/frameindex.htm
        //page 369 - 370

        float J_zero_of_X = 0f;

        //Test to see if the bessel functions are valid
        //Has to be above -3
        if (x <= -3f)
        {
            Debug.Log("smaller");
        }


        //9.4.1
        //-3 <= x <= 3
        if (x <= 3f)
        {
            //Ignored the small rest term at the end
            J_zero_of_X =
                1f -
                    2.2499997f * Mathf.Pow(x / 3f, 2f) +
                    1.2656208f * Mathf.Pow(x / 3f, 4f) -
                    0.3163866f * Mathf.Pow(x / 3f, 6f) +
                    0.0444479f * Mathf.Pow(x / 3f, 8f) -
                    0.0039444f * Mathf.Pow(x / 3f, 10f) +
                    0.0002100f * Mathf.Pow(x / 3f, 12f);
        }
        //9.4.3
        //3 <= x <= infinity
        else
        {
            //Ignored the small rest term at the end
            float f_zero =
                0.79788456f -
                    0.00000077f * Mathf.Pow(3f / x, 1f) -
                    0.00552740f * Mathf.Pow(3f / x, 2f) -
                    0.00009512f * Mathf.Pow(3f / x, 3f) -
                    0.00137237f * Mathf.Pow(3f / x, 4f) -
                    0.00072805f * Mathf.Pow(3f / x, 5f) +
                    0.00014476f * Mathf.Pow(3f / x, 6f);

            //Ignored the small rest term at the end
            float theta_zero =
                x -
                    0.78539816f -
                    0.04166397f * Mathf.Pow(3f / x, 1f) -
                    0.00003954f * Mathf.Pow(3f / x, 2f) -
                    0.00262573f * Mathf.Pow(3f / x, 3f) -
                    0.00054125f * Mathf.Pow(3f / x, 4f) -
                    0.00029333f * Mathf.Pow(3f / x, 5f) +
                    0.00013558f * Mathf.Pow(3f / x, 6f);

            //Should be cos and not acos
            J_zero_of_X = Mathf.Pow(x, -1f / 3f) * f_zero * Mathf.Cos(theta_zero);
        }

        return J_zero_of_X;
    }

    void CreateWaterWakesWithMouse()
    {
        //Fire ray from the current mouse position
        if (Input.GetKey(KeyCode.Mouse0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {

                //Convert the mouse position from global to local
                Vector3 localPos = transform.InverseTransformPoint(hit.point);

                //Loop through all the vertices of the water mesh
                for (int j = 0; j < arrayLength; j++)
                {
                    for (int i = 0; i < arrayLength; i++)
                    {
                        //Find the closest vertice within a certain distance from the mouse
                        float sqrDistanceToVertice = (height[j][i] - localPos).sqrMagnitude;

                        //If the vertice is within a certain range
                        float sqrDistance = 0.4f * 0.4f;

                        if (sqrDistanceToVertice < sqrDistance && hit.point.y < 10)
                        {
                            //Get a smaller value the greater the distance is to make it look better
                            float distanceCompensator = 1 - (sqrDistanceToVertice / sqrDistance);

                            //Add the force that now depends on how far the vertice is from the mouse
                            source[j][i].y += -0.08f * distanceCompensator;
                        }
                    }
                }
            }
        }
    }

    void Start () {
        waterMeshFilter = this.GetComponent<MeshFilter>();
        // Water mesh array
        List<Vector3[]> height_tmp = GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);

        waterMesh = waterMeshFilter.mesh;

        // Add box collider
        BoxCollider boxCollider = this.GetComponent<BoxCollider>();
        boxCollider.center = new Vector3(waterWidth / 2f, 0f, waterWidth / 2f);
        boxCollider.size = new Vector3(waterWidth, 0.1f, waterWidth);

        MeshCollider meshCollider = this.GetComponent<MeshCollider>();
        meshCollider.transform.Translate(5, 0, 5);


        transform.position = new Vector3(-waterWidth / 2f, 0f, -waterWidth / 2f);

        storedKernelArray = new float[P * 2 + 1, P * 2 + 1];
        PrecomputeKernelValues();

        //Init the arrays we need
        //These are now filled with heights at 0
        height = height_tmp.ToArray();
        //Need to clone these
        previousHeight = CloneList(height);
        verticalDerivative = CloneList(height);
        source = CloneList(height);
        obstruction = CloneList(height);
        heightDifference = CloneList(height);

        //Create this once here, so we dont need to create it each update
        unfolded_verts = new Vector3[height.Length * height.Length];

        arrayLength = height.Length;

        //Add obstruction when the wave hits the walls
        for (int j = 0; j < arrayLength; j++)
        {
            for (int i = 0; i < arrayLength; i++)
            {
                if (j == 0 || j == arrayLength - 1 || i == 0 || i == arrayLength - 1)
                {
                    obstruction[j][i].y = 0f;
                }
                else
                {
                    obstruction[j][i].y = 1f;
                }
            }
        }
    }

    void Update()
    {
        //Move water wakes
        updateTimer += Time.deltaTime;

        if (updateTimer > 0.02f)
        {
            MoveWater(0.02f);
            updateTimer = 0f;
        }

        CreateWaterWakesWithMouse();
    }

}
