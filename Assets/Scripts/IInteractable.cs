public interface IInteractable
{
    // ① 実行される処理本体（調べる、開けるなど）
    void Interact(); 

    // ② 画面に表示するテキスト（「調べる」「開ける」など）を返す機能
    string GetInteractPrompt(); 
}