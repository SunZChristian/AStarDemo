using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int G { get; set; } // 从起点到当前节点的代价
    public int H { get; set; } // 启发式估计到终点的代价
    public int F => G + H;     // 总代价
    public Plot ParentPlot;

    public bool isObstacle;

    [SerializeField] SpriteRenderer plotRenderer;

    public void Initialize(int x,int y)
    {
        X = x;
        Y = y;
    }

    public void SetPlotState(GameEntry.EOperateType eOperateType)
    {
        if (eOperateType == GameEntry.EOperateType.Null)
        {
            isObstacle = false;
            plotRenderer.color = Color.white;
            return;
        }

        if (eOperateType == GameEntry.EOperateType.Obstacle)
        {
            isObstacle = !isObstacle;
            plotRenderer.color = isObstacle ? Color.gray : Color.white;
            return;
        }

        if (eOperateType == GameEntry.EOperateType.StartPoint)
        {
            if (isObstacle)
                return;
            plotRenderer.color = Color.yellow;
            return;
        }

        if (eOperateType == GameEntry.EOperateType.EndPoint)
        {
            if (isObstacle)
                return;
            plotRenderer.color = Color.red;
            return;
        }

        if (eOperateType == GameEntry.EOperateType.Path)
        {
            if (isObstacle)
                return;
            plotRenderer.color = Color.green;
            return;
        }
    }
}
