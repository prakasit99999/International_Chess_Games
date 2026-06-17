using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mock GameAPI: แกล้งทำเป็นว่า Server ตอบกลับมาทันที
public class MockGameAPI : GameAPI
{
    public bool CreateGameCalled = false;
    public bool EndGameCalled = false;
    public int MockGameId = 999; // ID ปลอม

    // ต้องแก้ GameAPI ตัวจริงให้เป็น virtual public IEnumerator... ก่อนนะครับ
    public new virtual IEnumerator CreateGame(GameCreateDto dto, Action<int> onSuccess)
    {
        CreateGameCalled = true;
        yield return null; // รอ 1 เฟรมเหมือนของจริง
        onSuccess?.Invoke(MockGameId); // ส่ง ID ปลอมกลับไป
    }

    public new virtual IEnumerator EndGame(GameResultDto dto, Action<int> onSuccess = null)
    {
        EndGameCalled = true;
        yield return null;
        onSuccess?.Invoke(dto.GameId);
    }
}

// Mock MovesAPI: แกล้งทำเป็นว่ารับ Move Batch สำเร็จ
public class MockMovesAPI : MovesAPI
{
    public bool BatchSent = false;
    public int MoveCountReceived = 0;

    public new virtual IEnumerator SendMovesBatch(List<MoveCreateDto> moves, Action<bool> onComplete)
    {
        BatchSent = true;
        MoveCountReceived = moves.Count;
        yield return null;
        onComplete?.Invoke(true); // ตอบกลับว่า Success
    }
}