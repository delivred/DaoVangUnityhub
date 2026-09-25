namespace DaoVang.Hook
{
    public enum HookState
    {
        /// <summary>Móc đang đu qua lại chờ người chơi chạm màn hình.</summary>
        Swinging,
        /// <summary>Móc đang duỗi xuống dưới.</summary>
        Extending,
        /// <summary>Móc đang thu về, không mang vật phẩm.</summary>
        Retracting,
        /// <summary>Móc đang thu về, có mang theo vật phẩm.</summary>
        RetractingWithItem
    }
}
