﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    /// <summary>
    /// 缓存UI集合
    /// </summary>
    private Dictionary<string, GameObject> UIs = new Dictionary<string, GameObject>();
    /// <summary>
    /// UI分组集合
    /// </summary>
    private Dictionary<string, Transform> UIGroups = new Dictionary<string, Transform>();
    private Transform ui_Transform;

    void Awake()
    {
        ui_Transform = this.transform.parent.Find("UI");
    }

    /// <summary>
    /// 设置UI分组
    /// </summary>
    /// <param name="group"></param>
    public void SetGroup(List<string> group)
    {
        for (int i = 0; i < group.Count; i++)
        {
            GameObject go = new GameObject("Group-" + group[i]);
            go.transform.SetParent(ui_Transform, false);
            UIGroups.Add(group[i], go.transform);
        }
    }

    /// <summary>
    /// 获取UI分组
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    Transform GetGroup(string group)
    {
        if (!UIGroups.ContainsKey(group))
            Debug.LogError("Group is Not Exist");
        return UIGroups[group];
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    /// <param name="uiName">UI名称</param>
    /// <param name="group">分组类型名称</param>
    /// <param name="luaName">Lua脚本名称</param>
    public void ShowUI(string uiName,string group,string luaName)
    {
        GameObject ui = null;
        if (UIs.TryGetValue(uiName, out ui)) //查找集合是否已经加载过
        {
            UILogic uiLogic = ui.GetComponent<UILogic>();
            uiLogic.OnOpen();
            return;
        }

        Manager.Resourece.LoadUI(uiName, (UnityEngine.Object obj) =>
        {
            ui =Instantiate(obj) as GameObject;
            UIs.Add(uiName,ui);
            Transform parent = GetGroup(group);
            ui.transform.SetParent(parent, false);
            UILogic uiLogic = ui.AddComponent<UILogic>();
            uiLogic.Init(luaName);
            uiLogic.OnOpen();
        });
    }
}
