using UnityEngine;
using UnityEngine.EventSystems;

namespace AuraKnight.UI
{
    /// <summary>Tells the credits auto-scroll when the player is dragging the list by hand.</summary>
    public sealed class CreditsDragProbe : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public bool Dragging { get; private set; }

        public void OnBeginDrag(PointerEventData eventData) => Dragging = true;
        public void OnEndDrag(PointerEventData eventData) => Dragging = false;
    }
}
