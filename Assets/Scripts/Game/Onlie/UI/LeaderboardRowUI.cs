using TMPro;
using UnityEngine;

/// Script สำหรับ Row Prefab ในตาราง Leaderboard
public class LeaderboardRowUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text rankText;
    public TMP_Text usernameText;
    public TMP_Text ratingText;
    public TMP_Text winText;
    public TMP_Text drawText;
    public TMP_Text loseText;

    /// ตั้งค่าข้อมูลสำหรับแถวนี้
    public void SetData(LeaderboardResponse data)
    {
        if (rankText != null) rankText.text = data.rank.ToString();
        if (usernameText != null) usernameText.text = data.username;
        if (ratingText != null) ratingText.text = data.rating.ToString();
        if (winText != null) winText.text = data.w.ToString();
        if (drawText != null) drawText.text = data.d.ToString();
        if (loseText != null) loseText.text = data.l.ToString();
    }

    /// ตั้งค่าข้อมูลด้วย parameters แยก
    public void SetData(int rank, string username, int rating, int win, int draw, int lose)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (usernameText != null) usernameText.text = username;
        if (ratingText != null) ratingText.text = rating.ToString();
        if (winText != null) winText.text = win.ToString();
        if (drawText != null) drawText.text = draw.ToString();
        if (loseText != null) loseText.text = lose.ToString();
    }
}
