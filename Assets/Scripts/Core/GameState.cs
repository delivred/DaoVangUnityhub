namespace DaoVang.Core
{
    /// <summary>
    /// 5 trạng thái của game: Menu -> LevelSelect -> Playing -> (LevelComplete | GameOver)
    /// LevelSelect là màn chọn level, chen giữa Menu và Playing.
    /// </summary>
    public enum GameState
    {
        Menu,
        LevelSelect,
        Playing,
        LevelComplete,
        GameOver,
        Shop
    }
}
