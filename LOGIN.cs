using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject registerPanel;
    internal static object instance;

    private void Start()
    {
        // Initially, only the login panel is active
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
    }

    public void ShowRegisterUI()
    {
        // Hide the login panel and show the register panel
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    public void ShowLoginUI()
    {
        // Hide the register panel and show the login panel
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }
}
