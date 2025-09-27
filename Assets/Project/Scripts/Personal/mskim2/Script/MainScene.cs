using UnityEngine;
using WhaleShark.Gameplay;

public class MainScene : MonoBehaviour
{
    public void StartPrologue()
    {
        GameManager.Instance.LoadScene("Prologue");
    }
}
