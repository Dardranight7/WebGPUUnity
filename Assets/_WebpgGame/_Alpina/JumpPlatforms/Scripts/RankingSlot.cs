using TMPro;
using UnityEngine;

public class RankingSlot : MonoBehaviour
{
    public TextMeshProUGUI username, points, position;
    public Transform bgPlayer, iconPlayer;
    
    public void SetValues(string name, string points, string position, bool isPlayer = false)
    {
        this.username.text = name;
        this.points.text = points;
        this.position.text = position;
        bgPlayer.gameObject.SetActive(isPlayer);
        iconPlayer.gameObject.SetActive(isPlayer);
    }
}
