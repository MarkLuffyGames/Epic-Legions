using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class HLTextCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public void OnPointerEnter(PointerEventData e) => CursorThemeHL.Instance?.Apply(HLCursorStyle.Text);
    public void OnPointerExit(PointerEventData e) => CursorThemeHL.Instance?.Apply(HLCursorStyle.Default);
    public void OnSelect(BaseEventData e) => CursorThemeHL.Instance?.Apply(HLCursorStyle.Text);
    public void OnDeselect(BaseEventData e) => CursorThemeHL.Instance?.Apply(HLCursorStyle.Default);
}
