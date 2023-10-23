using UnityEngine;
using UnityEngine.UI;

public class BottomScript : MonoBehaviour
{
    public Button button;
    // Start is called before the first frame update
    void Start()
    {
        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
    {
        Debug.Log("You have clicked the button!");
    }
}
