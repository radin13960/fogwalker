using FogWalker.Controls;
using FogWalker.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FogWalker.UI.HUD
{
    /// <summary>دکمه Pause روی HUD: درخواست توقف از InputManager (معادل Escape/Start).</summary>
    public sealed class PauseButtonProxy : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (ServiceLocator.TryGet(out InputManager input))
                input.RequestPause();
        }
    }
}
