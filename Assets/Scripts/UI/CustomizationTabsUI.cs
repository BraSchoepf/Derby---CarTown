using UnityEngine;
using UnityEngine.UI;

public class CustomizationTabsUI : MonoBehaviour
{
    public enum Tab { None, Car, Color, Wheels }

    [Header("Botones de tabs")]
    public GameObject tabButtonsContainer;
    public Button carTabButton;
    public Button colorTabButton;
    public Button wheelsTabButton;

    [Header("Sub-paneles")]
    public GameObject carSubPanel;
    public GameObject colorSubPanel;
    public GameObject wheelsSubPanel;

    [Header("Botón de cerrar")]
    public Button closeButton;

    public int ownerPlayerIndex = 1;

    // Orden de navegación por teclado entre los botones de tabs
    static readonly Tab[] TabOrder = { Tab.Car, Tab.Color, Tab.Wheels };
    int hoveredTabIndex = 0;

    public Tab CurrentTab { get; private set; } = Tab.None;
    public bool IsSubPanelOpen => CurrentTab != Tab.None;

    void Awake()
    {
        carTabButton.onClick.AddListener(() => OpenTab(Tab.Car));
        colorTabButton.onClick.AddListener(() => OpenTab(Tab.Color));
        wheelsTabButton.onClick.AddListener(() => OpenTab(Tab.Wheels));

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCurrentPanel);

        OpenTab(Tab.Car);
    }

    public void OpenTab(Tab tab)
    {
        CurrentTab = tab;
        tabButtonsContainer.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        carSubPanel.SetActive(tab == Tab.Car);
        colorSubPanel.SetActive(tab == Tab.Color);
        wheelsSubPanel.SetActive(tab == Tab.Wheels);
    }

    public void CloseCurrentPanel()
    {
        CurrentTab = Tab.None;
        ShowTabButtons();
    }

    void ShowTabButtons()
    {
        tabButtonsContainer.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        carSubPanel.SetActive(false);
        colorSubPanel.SetActive(false);
        wheelsSubPanel.SetActive(false);

        hoveredTabIndex = 0;
        HighlightHoveredTab();
    }

    // --- Navegación por teclado (nuevo) ---

    public void MoveTabHover(int direction)
    {
        if (direction == 0) return;
        hoveredTabIndex = ((hoveredTabIndex + direction) % TabOrder.Length + TabOrder.Length) % TabOrder.Length;
        HighlightHoveredTab();
    }

    public void ConfirmHoveredTab()
    {
        OpenTab(TabOrder[hoveredTabIndex]);
    }

    void HighlightHoveredTab()
    {
        // Placeholder simple: podés reemplazar esto por un resaltado visual real
        // (por ejemplo, un outline o escala en el botón correspondiente)
        Button[] buttons = { carTabButton, colorTabButton, wheelsTabButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            var colors = buttons[i].colors;
            colors.normalColor = i == hoveredTabIndex ? Color.yellow : Color.white;
            buttons[i].colors = colors;
        }
    }
}