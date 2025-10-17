using TMPro;
using UnityEngine;

public class RankingSlot : MonoBehaviour
{
    public TextMeshProUGUI username, points, position;
    
    public void SetValues(string name, string points, string position)
    {
        this.username.text = name;
        this.points.text = points;
        this.position.text = position;
    }
}
