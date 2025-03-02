using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;

public class MatrixCompare : MonoBehaviour
{
    [SerializeField] private GameObject cubeSpacePrefab;
    [SerializeField] private GameObject cubeModelPrefab;
    private Matrix4x4[] modelMatrices;
    private Matrix4x4[] spaceMatrices;
    private List<Transform> modelCubes = new List<Transform>();
    private List<Vector3> spaceCubes = new List<Vector3>();

    private GameObject modelParent;
    private GameObject spaceParent;

    private List<Matrix4x4> matchList = new List<Matrix4x4>();
    private int currentVisual = -1;
    private float timeToAnimate = 3f;
    private float timeToAwait = 2f;

    void Start()
    {
        LoadMatrices();
        SetupParents();
        SpawnCubes();
        FindMatches();
        ExportResultToFile();
        VisualCycle();
    }

    void SetupParents()
    {
        modelParent = new GameObject("ModelParent");
        spaceParent = new GameObject("SpaceParent");
    }

    void LoadMatrices()
    {
        TextAsset modelJson = Resources.Load<TextAsset>("model");
        TextAsset spaceJson = Resources.Load<TextAsset>("space");

        modelMatrices = JsonConvert.DeserializeObject<Matrix4x4[]>(modelJson.text);
        spaceMatrices = JsonConvert.DeserializeObject<Matrix4x4[]>(spaceJson.text);
    }

    private void SpawnCubes()
    {
        for (int i = 0; i < spaceMatrices.Length; i++)
        {
            Vector3 pos = spaceMatrices[i].GetPosition();
            Quaternion rot = spaceMatrices[i].rotation;
            GameObject cube = Instantiate(cubeSpacePrefab, pos, rot, spaceParent.transform);
            cube.name = $"SpaceCube_{i}";
            spaceCubes.Add(cube.transform.position);
        }

        modelParent.transform.position = modelMatrices[0].GetPosition();
        modelParent.transform.rotation = modelMatrices[0].rotation;

        for (int i = 0; i < modelMatrices.Length; i++)
        {
            Vector3 pos = modelMatrices[i].GetPosition();
            Quaternion rot = modelMatrices[i].rotation;
            GameObject cube = Instantiate(cubeModelPrefab, pos, rot, modelParent.transform);
            cube.name = $"ModelCube_{i}";
            modelCubes.Add(cube.transform);
        }

        Debug.Log($"modelCubes {modelCubes.Count} and spaceCubes {spaceCubes.Count}");
    }

    void FindMatches()
    {
        foreach (var spaceMatrix in spaceMatrices)
        {
            modelParent.transform.position = spaceMatrix.GetPosition();
            modelParent.transform.rotation = spaceMatrix.rotation;

            bool equal = true;
            foreach (var model in modelCubes)
            {
                if (!IsPositionWithinTolerance(model.position, spaceCubes, 0.1f))
                {
                    equal = false;
                    break;
                }
            }

            if (equal)
            {
                matchList.Add(spaceMatrix);
            }
        }

        Debug.Log("Match count " + matchList.Count);
    }

    private bool IsPositionWithinTolerance(Vector3 position, List<Vector3> positions, float tolerance)
    {
        foreach (var pos in positions)
        {
            if (Vector3.Distance(position, pos) <= tolerance)
            {
                return true;
            }
        }

        return false;
    }


    void ExportResultToFile()
    {
        SerializableMatrix4x4[] serializableMatrices = new SerializableMatrix4x4[matchList.Count];
        for (int i = 0; i < matchList.Count; i++)
        {
            serializableMatrices[i] = new SerializableMatrix4x4(matchList[i]);
        }

        var jsonResult = JsonConvert.SerializeObject(serializableMatrices);

        string path = Path.Combine(Application.persistentDataPath, "match.json");
        File.WriteAllText(path, jsonResult);
        Debug.Log("Success save to " + path);
    }

    private void VisualCycle()
    {
        if (matchList.Count == 0) return;

        currentVisual++;
        if (currentVisual >= matchList.Count) currentVisual = 0;


        modelParent.transform.DOMove(matchList[currentVisual].GetPosition(), timeToAnimate);
        modelParent.transform.DORotate(matchList[currentVisual].rotation.eulerAngles, timeToAnimate);
        Invoke(nameof(VisualCycle), timeToAnimate + timeToAwait);
    }
}

[System.Serializable]
public class SerializableMatrix4x4
{
    public float m00, m10, m20, m30;
    public float m01, m11, m21, m31;
    public float m02, m12, m22, m32;
    public float m03, m13, m23, m33;

    public SerializableMatrix4x4(Matrix4x4 matrix)
    {
        m00 = matrix.m00;
        m01 = matrix.m01;
        m02 = matrix.m02;
        m03 = matrix.m03;
        m10 = matrix.m10;
        m11 = matrix.m11;
        m12 = matrix.m12;
        m13 = matrix.m13;
        m20 = matrix.m20;
        m21 = matrix.m21;
        m22 = matrix.m22;
        m23 = matrix.m23;
        m30 = matrix.m30;
        m31 = matrix.m31;
        m32 = matrix.m32;
        m33 = matrix.m33;
    }
}