using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对话序列数据资产（ScriptableObject）
/// 用来存储一整段对话的内容、配置以及选项
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Header("--- 对话基础信息 ---")]
    public string sequenceID;          // 对话唯一ID
    public string sequenceName;        // 对话名称（如：室友求帮忙）

    [Header("--- 对话文本内容 ---")]
    [TextArea(3, 5)]                   // 让输入框变大，方便换行输入
    public List<string> dialogueLines = new List<string>();
}