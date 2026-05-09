using UnityEngine;
using UnityEngine.UIElements;

public class ыыыы : MonoBehaviour
{
    private VisualElement root;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var scroll = root.Q<ScrollView>("Scroll");

        // когда ScrollView получил реальную высоту
        scroll.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            var viewportHeight = scroll.contentViewport.resolvedStyle.height;

            scroll.contentContainer.style.minHeight = viewportHeight;
            scroll.contentContainer.style.flexGrow = 1;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
