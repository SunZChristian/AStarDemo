using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameEntry : MonoBehaviour
{
    public enum EOperateType
    {
        Null,
        Obstacle,
        StartPoint,
        EndPoint,
        Path,
        Reset
    }

    [SerializeField] int width;
    [SerializeField] int height;

    [SerializeField] Toggle nullToggle;
    [SerializeField] Toggle obstacleToggle;
    [SerializeField] Toggle startPointToggle;
    [SerializeField] Toggle endPointToggle;
    [SerializeField] Toggle resetToggle;
    [SerializeField] Button resetAllButton;
    [SerializeField] Button findPathButton;

    [SerializeField] EOperateType operateType = EOperateType.Null;

    GameObject startPoint;
    GameObject endPoint;
    GameObject[,] map;
    List<Plot> path;
    int[,] neighborHelper = new int[,]
                {
                    { 0, 1 },
                    { 0, -1 },
                    { -1, 0 },
                    { 1, 0 }
                };

    // Start is called before the first frame update
    void Start()
    {
        nullToggle.onValueChanged.AddListener((b) => { if (b) { operateType = EOperateType.Null; } });
        obstacleToggle.onValueChanged.AddListener((b) => { if (b) { operateType = EOperateType.Obstacle; } });
        startPointToggle.onValueChanged.AddListener((b) => { if (b) { operateType = EOperateType.StartPoint; } });
        endPointToggle.onValueChanged.AddListener((b) => { if (b) { operateType = EOperateType.EndPoint; } });
        resetToggle.onValueChanged.AddListener((b) => { if (b) { operateType = EOperateType.Reset; } });
        findPathButton.onClick.AddListener(FindPath);
        resetAllButton.onClick.AddListener(ResetAllMap);
        CreateMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //创建一条从屏幕射出的射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //获得射线检测命中到的目标
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);
            if (hit.collider != null && hit.collider.CompareTag("Plot"))
            {
                Debug.Log("点击到");
                Process(hit.transform.parent.gameObject);
            }
        }
    }

    private void CreateMap()
    {
        map = new GameObject[width, height];

        GameObject prefab = Resources.Load<GameObject>("Plot");
        for (int w = 0; w < width; w++)
        {
            for (int h = 0; h < height; h++)
            {
                GameObject plot = Instantiate<GameObject>(prefab, new Vector3(w, h, 0), Quaternion.identity, transform);
                plot.GetComponent<Plot>().Initialize(w, h);
                map[w, h] = plot;
            }
        }

    }

    private void Process(GameObject plot)
    {
        if (operateType == EOperateType.Null)
            return;

        if (operateType == EOperateType.Obstacle)
        {
            if (startPoint == plot || endPoint == plot)
                return;
            plot.GetComponent<Plot>().SetPlotState(operateType);
            return;
        }

        if (operateType == EOperateType.StartPoint)
        {
            if (startPoint == plot || endPoint == plot)
                return;

            if (startPoint != null)
                startPoint.GetComponent<Plot>().SetPlotState(EOperateType.Null);

            startPoint = plot;
            startPoint.GetComponent<Plot>().SetPlotState(operateType);
            return;
        }

        if (operateType == EOperateType.EndPoint)
        {
            if (startPoint == plot || endPoint == plot)
                return;

            if (endPoint != null)
                endPoint.GetComponent<Plot>().SetPlotState(EOperateType.Null);
            endPoint = plot;
            endPoint.GetComponent<Plot>().SetPlotState(operateType);
            return;
        }

        if (operateType == EOperateType.Reset)
        {
            plot.GetComponent<Plot>().SetPlotState(operateType);
        }
    }

    private void FindPath()
    {
        Debug.Log("开始寻路");

        ClearPath();

        if (startPoint == null || endPoint == null)
        {
            Debug.Log("起始点不存在");
            return;
        }

        List<Plot> openList = new List<Plot>();
        List<Plot> closeList = new List<Plot>();

        //把起点放入openlist
        var startPlot = startPoint.GetComponent<Plot>();
        startPlot.H = CalculateH(startPlot);
        openList.Add(startPlot);

        bool findPath = false;
        while (openList.Count > 0)
        {
            int minF = int.MaxValue;
            Plot minPlot = null;
            for (int i = 0; i < openList.Count; i++)
            {
                if (openList[i].F < minF)
                {
                    minPlot = openList[i];
                    minF = minPlot.F;
                }
            }

            if (minPlot == endPoint.GetComponent<Plot>())
            {
                findPath = true;
                break;
            }

            if (minPlot != null)
            {
                closeList.Add(minPlot);
                openList.Remove(minPlot);
            }

            Plot[] neighbors = GetNeighbors(minPlot);
            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighbor = neighbors[i];
                if (neighbor == null)
                    continue;
                if (neighbor.isObstacle)
                    continue;
                if (closeList.Contains(neighbor))
                    continue;
                if (!openList.Contains(neighbor))
                {
                    openList.Add(neighbor);                    
                    neighbor.ParentPlot = minPlot;
                    neighbor.G = minPlot.G + 1;
                    neighbor.H = CalculateH(neighbor);  
                }
                else
                {
                    if (neighbor.G < minPlot.G)
                    {
                        neighbor.ParentPlot = minPlot;
                        neighbor.G = minPlot.G + 1;
                    }
                }
            }
        }

        if (findPath)
        {
            Debug.Log("找到了！");
            //下面根据
            var curPoint = endPoint.GetComponent<Plot>();
            while (curPoint != null)
            {
                path.Add(curPoint);
                curPoint = curPoint.ParentPlot;               
            }

            //除去终点和起点，剩下的路径可视化出来
            for (int i = 1; i < path.Count-1; i++)
            {
                path[i].SetPlotState(EOperateType.Path);
            }
        }
        else
        {
            Debug.Log("没有找到合适的路径");
        }
    }

    private Plot[] GetNeighbors(Plot plot)
    {
        //这里只考虑上下左右

        Plot[] plots = new Plot[neighborHelper.GetLength(0)];

        int idx = 0;

        for (int i = 0; i < neighborHelper.GetLength(0); i++)
        {
            int posX = neighborHelper[i, 0] + plot.X;
            int posY = neighborHelper[i, 1]+plot.Y;
            if (IsValid(posX, posY))
            {
                var neighbor = map[posX, posY].GetComponent<Plot>();
                plots[idx] = neighbor;
                idx++;
            }
        }

        return plots;
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private int CalculateH(Plot plot)
    {
        if (endPoint == null)
            return -1;
        var endPlot = endPoint.GetComponent<Plot>();
        return Mathf.Abs(plot.X - endPlot.X) + Mathf.Abs(plot.Y - endPlot.Y);
    }

    private void ClearPath()
    {
        if (path == null)
            path = new List<Plot>();

        for (int i = 1; i < path.Count-1; i++)
        {
            path[i].SetPlotState(EOperateType.Null);
        }
        path.Clear();

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                var plot = map[i, j].GetComponent<Plot>();
                plot.G = 0;
                plot.H = 0;
            }
        }
    }

    private void ResetAllMap()
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                var plot = map[i, j].GetComponent<Plot>();
                plot.G = 0;
                plot.H = 0;
                plot.SetPlotState(EOperateType.Null);
            }
        }
    }
}
