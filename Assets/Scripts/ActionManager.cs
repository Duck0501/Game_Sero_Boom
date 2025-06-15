using System.Collections.Generic;

public class ActionManager
{
    private Stack<PlayerAction> actionStack; // Stack để lưu các hành động

    // Khởi tạo Stack
    public ActionManager()
    {
        actionStack = new Stack<PlayerAction>();
    }

    // Thêm hành động mới vào Stack
    public void AddAction(PlayerAction action)
    {
        if (action != null)
        {
            actionStack.Push(action);
        }
    }

    // Hoàn tác hành động cuối cùng
    public PlayerAction Undo()
    {
        if (actionStack.Count > 0)
        {
            return actionStack.Pop();
        }
        return null; // Trả về null nếu không có hành động để hoàn tác
    }

    // Kiểm tra xem có hành động để hoàn tác không
    public bool CanUndo()
    {
        return actionStack.Count > 0;
    }

    // Xóa toàn bộ Stack (nếu cần)
    public void Clear()
    {
        actionStack.Clear();
    }
}

// Lớp đại diện cho hành động của người chơi
public class PlayerAction
{
    public enum ActionType
    {
        Move,
        EatBanana,
        PushBanana,
        EatMedicine,
        PushMedicine
    }

    public ActionType Type { get; private set; }
    public UnityEngine.Vector2 PositionBefore { get; private set; }
    public UnityEngine.Vector2 PositionAfter { get; private set; }

    public PlayerAction(ActionType type, UnityEngine.Vector2 positionBefore, UnityEngine.Vector2 positionAfter = new UnityEngine.Vector2())
    {
        Type = type;
        PositionBefore = positionBefore;
        PositionAfter = positionAfter;
    }
}