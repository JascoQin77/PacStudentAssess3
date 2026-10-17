using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] AudioSource feedbackAudio;
    [SerializeField] AudioClip highlightClip;
    [SerializeField] AudioClip clickClip;
    bool hovered, selected;

    public void Configure(AudioSource source, AudioClip highlight, AudioClip click)
    {
        feedbackAudio = source;
        highlightClip = highlight;
        clickClip = click;
    }

    void Sound(AudioClip clip, float volume)
    {
        if (feedbackAudio != null && clip != null) feedbackAudio.PlayOneShot(clip, volume);
    }

    void Refresh() => transform.localScale = Vector3.one * (hovered || selected ? 1.015f : 1f);
    public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); Sound(highlightClip, 0.25f); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
    public void OnSelect(BaseEventData data) { selected = true; Refresh(); Sound(highlightClip, 0.25f); }
    public void OnDeselect(BaseEventData data) { selected = false; Refresh(); }
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        transform.localScale = Vector3.one * 0.985f;
        Sound(clickClip, 0.55f);
    }
    public void OnPointerUp(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Refresh(); }
    public void OnSubmit(BaseEventData data) => Sound(clickClip, 0.55f);
    void OnDisable() { hovered = selected = false; transform.localScale = Vector3.one; }
}
