namespace App
{
    /// <summary>
    /// AutoPlayAgent が操作対象に要求するインターフェース。
    /// Dependency Inversion Principle に基づき、AutoPlayAgent は
    /// GameMainController 具象クラスを知る必要がない。
    /// </summary>
    public interface IAutoPlayTarget
    {
        /// <summary>現在入力を受け付けられる状態か（Playing ステート中のみ true）</summary>
        bool AcceptsInput { get; }

        void OnInputMoveLeft();
        void OnInputMoveRight();
        void OnInputRotateCW();
        void OnInputRotateCCW();
        void OnInputSoftDrop();
        void OnInputSoftDropEnd();
    }
}
