using System;
using System.Collections.Generic;
using UnityEngine;

public class ECSManager : MonoBehaviour {

    // debug on screen text
    void OnGUI()
    {
        var styleLeft = new GUIStyle(GUI.skin.box);
        styleLeft.alignment = TextAnchor.MiddleLeft;
        GUI.Box(new Rect(Screen.width-200, 0, 200, 20), "FPS: " + _frameRate.ToString(), styleLeft);
    }

    static private float _frameRate = 0; // debug

    #region Public API

    public Config Config { get { return _config; } }

    public List<ISystem> AllSystems { get { return _allSystems; } }

    public void CreateShape(uint id, Config.ShapeConfig entityConfig)
    {
        if (_gameObjectsForDisplay.ContainsKey(id))
        {
            DestroyShape(id);
        }
        GameObject instance;
        switch (entityConfig.shape)
        {
            case Config.Shape.Circle:
                instance = Instantiate(_circlePrefab);
                break;
            case Config.Shape.Square:
                instance = Instantiate(_squarePrefab);
                break;
            default:
                Debug.LogError("Unhandled shape " + entityConfig.shape);
                instance = new GameObject();
                break;
        }
        instance.transform.localScale *= entityConfig.size;
        instance.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(id/10.0f, 1, 1); // assumes there's just 10 shapes at the same time
        _gameObjectsForDisplay[id] = instance;
    }

    public void DestroyShape(uint id)
    {
        Destroy(_gameObjectsForDisplay[id]);
    }

    public void UpdateShapePosition(uint id, Vector2 position)
    {
        _gameObjectsForDisplay[id].transform.position = position;
    }

    public void UpdateShapeSize(uint id, float size)
    {
        _gameObjectsForDisplay[id].transform.localScale = Vector2.one * size;
    }

    #endregion

    #region System Management
    private List<ISystem> _allSystems = new List<ISystem>();

    private void FixedUpdate()
    {
        _frameRate = 1.0f / Time.deltaTime;
        foreach (var system in _allSystems)
        {
            system.UpdateSystem();
        }
    }

    private void Awake()
    {
        _allSystems = RegisterSystems.GetListOfSystems();
    }

    #endregion

    #region Singleton
    private static ECSManager _instance;
    public static ECSManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ECSManager>();
                if (_instance == null)
                {
                    Debug.LogError("Can't find instance in scene!!");
                }
            }
            return _instance;
        }
    }
    private ECSManager() { }
    #endregion

    #region Private attributes
    [SerializeField]
    private Config _config;

    [SerializeField]
    public CustomNetworkManager NetworkManager;

    [SerializeField]
    private GameObject _circlePrefab;
    [SerializeField]
    private GameObject _squarePrefab;

    private Dictionary<uint, GameObject> _gameObjectsForDisplay = new Dictionary<uint, GameObject>();
    #endregion
}

public interface ISystem
{
    void UpdateSystem();
    string Name { get; }
}

public interface IComponent
{
}

public class EntityComponent : IComponent
{
    public int GetRandomNumber() => randomNumber;
    private static readonly int randomNumber = 456834732; // random number to help GetHashCode have different values for class Types in TypeRegistry

    public uint id;
    public EntityComponent(uint id)
    {
        this.id = id;
    }

    public static implicit operator uint(EntityComponent e) => e.id;
    public static implicit operator EntityComponent(uint id)
    {
        return new EntityComponent(id);
    }
}

