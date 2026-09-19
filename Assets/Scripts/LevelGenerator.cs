using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] Transform manualLevel;
    [SerializeField] Transform tileParent;
    [SerializeField] Transform pelletParent;
    [SerializeField] Camera levelCamera;
    [SerializeField] float tileSize = 1f;
    [SerializeField] Sprite[] tileSprites;
    [SerializeField] Sprite pelletSprite;
    [SerializeField] RuntimeAnimatorController powerPelletController;
    GameObject spriteTemplate;

    readonly int[,] levelMap = {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    void Start()
    {
        if (manualLevel != null) Destroy(manualLevel.gameObject);
        var generatedRoot = NewGroup("GeneratedLevel").transform;
        tileParent = NewGroup("GeneratedWalls").transform;
        pelletParent = NewGroup("GeneratedPellets").transform;
        tileParent.SetParent(generatedRoot, false);
        pelletParent.SetParent(generatedRoot, false);
        spriteTemplate = new GameObject("GeneratedSpriteTemplate");
        spriteTemplate.AddComponent<SpriteRenderer>();
        spriteTemplate.SetActive(false);
        GenerateFullLevel();
        Destroy(spriteTemplate);
        FitCamera();
    }

    GameObject NewGroup(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go;
    }

    void GenerateFullLevel()
    {
        int rows = levelMap.GetLength(0), cols = levelMap.GetLength(1);
        int fullRows = rows * 2 - 1;
        int fullCols = cols * 2;
        int[,] fullMap = new int[fullRows, fullCols];
        for (int row = 0; row < fullRows; row++)
        for (int col = 0; col < fullCols; col++)
        {
            int sourceRow = row < rows ? row : fullRows - 1 - row;
            int sourceCol = col < cols ? col : fullCols - 1 - col;
            fullMap[row, col] = levelMap[sourceRow, sourceCol];
        }
        for (int row = 0; row < fullRows; row++)
        for (int col = 0; col < fullCols; col++)
        {
            float x = (col - (fullCols - 1) * 0.5f) * tileSize;
            float y = ((fullRows - 1) * 0.5f - row) * tileSize;
            Place(fullMap, row, col, x, y);
        }
    }

    void Place(int[,] map, int row, int col, float x, float y)
    {
        int value = map[row, col];
        if (value >= 1 && value <= 4 || value == 7 || value == 8)
        {
            string file = value == 1 ? "Wall_OutsideCorner.png" : value == 2 ? "Wall_Outside.png" : value == 3 ? "Wall_InsideCorner.png" : value == 4 ? "Wall_Inside.png" : value == 7 ? "Wall_TJunction.png" : "Wall_GhostExit.png";
            var go = Instantiate(spriteTemplate, tileParent);
            go.name = "Tile_" + row + "_" + col;
            go.SetActive(true);
            go.transform.localPosition = new Vector3(x, y, 0);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = FindTileSprite(file);
            renderer.sortingOrder = 2;
            go.transform.localRotation = Quaternion.Euler(0, 0, Rotation(map, value, row, col));
        }
        else if (value == 5 || value == 6)
        {
            var go = Instantiate(spriteTemplate, pelletParent);
            go.name = value == 5 ? "StandardPellet" : "PowerPellet";
            go.SetActive(true);
            go.transform.localPosition = new Vector3(x, y, -0.1f);
            go.transform.localScale = Vector3.one * (value == 5 ? 0.08f : 0.14f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = pelletSprite;
            renderer.sortingOrder = 3;
            if (value == 6 && powerPelletController != null)
            {
                var animator = go.AddComponent<Animator>();
                animator.runtimeAnimatorController = powerPelletController;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }
    }

    int Rotation(int[,] map, int value, int row, int col)
    {
        bool up = IsWall(map, row - 1, col);
        bool right = IsWall(map, row, col + 1);
        bool down = IsWall(map, row + 1, col);
        bool left = IsWall(map, row, col - 1);
        if (value == 2 || value == 4 || value == 8)
            return (up || down) && !(left || right) ? 90 : 0;
        if (value == 1 || value == 3)
        {
            if (up && right) return 0;
            if (right && down) return -90;
            if (down && left) return 180;
            if (left && up) return 90;
        }
        if (value == 7)
        {
            if (!down) return 0;
            if (!left) return -90;
            if (!up) return 180;
            if (!right) return 90;
        }
        return 0;
    }

    bool IsWall(int[,] map, int row, int col)
    {
        if (row < 0 || col < 0 || row >= map.GetLength(0) || col >= map.GetLength(1)) return false;
        int value = map[row, col];
        return value == 1 || value == 2 || value == 3 || value == 4 || value == 7 || value == 8;
    }

    Sprite FindTileSprite(string file)
    {
        string name = file.Replace(".png", "");
        if (tileSprites != null)
            foreach (var sprite in tileSprites)
                if (sprite != null && sprite.texture != null && sprite.texture.name == name) return sprite;
        return null;
    }

    void FitCamera()
    {
        if (levelCamera == null) levelCamera = Camera.main;
        if (levelCamera == null) return;
        levelCamera.orthographic = true;
        levelCamera.transform.position = new Vector3(0, 0, -10);
        levelCamera.orthographicSize = levelMap.GetLength(0) + 2f;
    }
}
