using UnityEngine;
using WorkFolder.PAP.Scripts.Agent.Part;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [SerializeField] private AbstractPartDataSO[] allBodyPartData;

    private void Awake() => Instance = this;

    public AbstractPartDataSO GetSOById(int id)
    {
        if (id < 0 || id >= allBodyPartData.Length) return null;
        return allBodyPartData[id];
    }

    public int GetIdBySO(AbstractPartDataSO so)
    {
        for (int i = 0; i < allBodyPartData.Length; i++)
        {
            if (allBodyPartData[i] == so) return i;
        }
        return -1;
    }
}