using System.Linq;
using App.Puyo;
using UnityEditor;
using UnityEngine;

namespace App.EditorTools
{
    /// <summary>
    /// PuyoBoard のセルサイズと原点を "Line" スプライトの bounds から自動計算して整列する。
    /// メニュー: WrestlePachi > PuyoBoard グリッド自動整列
    /// </summary>
    public static class PuyoBoardAutoAlignMenuItem
    {
        [MenuItem("WrestlePachi/PuyoBoard グリッド自動整列")]
        private static void AutoAlign()
        {
            var board = Object.FindObjectOfType<PuyoBoard>();
            if (board == null)
            {
                Debug.LogError("[AutoAlign] シーンに PuyoBoard が見つかりません");
                return;
            }

            // BoardRoot 配下の "Line" SpriteRenderer を探す
            var lineRenderer = board.transform.parent
                .GetComponentsInChildren<SpriteRenderer>()
                .FirstOrDefault(r => r.gameObject.name == "Line");

            if (lineRenderer == null)
            {
                Debug.LogError("[AutoAlign] 'Line' という名前の SpriteRenderer が見つかりません");
                return;
            }

            var bounds = lineRenderer.bounds;
            var cellW  = bounds.size.x / PuyoBoard.COLS;
            var cellH  = bounds.size.y / PuyoBoard.VISIBLE_ROWS;

            // SerializedObject 経由で private フィールド _cellSize を書き換える
            var so            = new SerializedObject(board);
            var cellSizeProp  = so.FindProperty("_cellSize");
            cellSizeProp.floatValue = cellW;
            so.ApplyModifiedProperties();

            // Transform の position を cell(0,0) 中心に合わせる
            Undo.RecordObject(board.transform, "PuyoBoard AutoAlign");
            board.transform.position = new Vector3(
                bounds.min.x + cellW * 0.5f,
                bounds.min.y + cellH * 0.5f,
                board.transform.position.z
            );

            EditorUtility.SetDirty(board);
            EditorUtility.SetDirty(board.transform);

            Debug.Log($"[AutoAlign] 完了: _cellSize={cellW:F4}, position={board.transform.position}");
        }
    }
}
