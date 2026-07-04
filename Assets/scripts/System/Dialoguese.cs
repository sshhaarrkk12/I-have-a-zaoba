using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对话树节点，每个节点是一个独立的 ScriptableObject
/// 右键 Assets → Create → EarlyClass8 → DialogueNode
///
/// 配置方式：
///   - 无选项：填 next，点击后跳到下一节点（留空=结束）
///   - 有选项：填 choices，每个选项拖入对应的下一节点
///   - 支持任意层级嵌套分支
/// </summary>
[CreateAssetMenu(fileName = "NewNode", menuName = "EarlyClass8/DialogueNode")]
public class DialogueNode : ScriptableObject
{
    [Header("备注（仅编辑器，不影响游戏）")]
    public string editorLabel;

    [Header("内容")]
    [Tooltip("角色名，留空默认'我'")]
    public string characterName = "我";
    [TextArea(2, 5)]
    public string text;
    public Sprite portrait;

    [Header("到达此节点时的属性变化")]
    public StatsDelta statsDelta;

    [Header("到达此节点时触发的事件（可选）")]
    public string triggerEventId;

    [Header("无选项时 → 点击后跳到这里（留空=结束）")]
    public DialogueNode next;

    [Header("有选项时 → 填 choices（填了 choices 则忽略上面的 next）")]
    public List<DialogueNodeChoice> choices = new List<DialogueNodeChoice>();
}

[System.Serializable]
public class DialogueNodeChoice
{
    [Tooltip("按钮文字")]
    public string label;

    [Header("选择后属性变化")]
    public StatsDelta statsDelta;

    [Header("选择后触发事件（可选）")]
    public string triggerEventId;

    [Header("选择后跳到哪个节点（留空=结束）")]
    public DialogueNode next;
}